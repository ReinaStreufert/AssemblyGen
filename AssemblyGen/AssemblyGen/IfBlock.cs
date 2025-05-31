using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Emit;
using System.Text;
using System.Threading.Tasks;

namespace AssemblyGen
{
    public partial class MethodGeneratorContext
    {
        private class IfBlock : Block, IIfBlock<Symbol>
        {
            public IfBlock(MethodGeneratorContext ctx, int level, List<Branch> branches) : base(ctx, level)
            {
                _OuterEmitList = ctx._EmitList;
                _EscapeLabel = ctx._Il.DefineLabel();
                _Branches = branches;
            }

            private List<IEmittable> _OuterEmitList;
            private List<Branch> _Branches;
            private bool _Continued = false;
            private Label _EscapeLabel;

            public void ElseIf(Symbol condition)
            {
                EnsureLevel();
                if (condition.Type != typeof(bool))
                    throw new ArgumentException($"{nameof(condition)} must be of type boolean");
                var branchBeginLabel = Ctx._Il.DefineLabel();
                var branchBody = new List<IEmittable>();
                Ctx._EmitList = _OuterEmitList;
                Ctx._Target.Put(ILExpressionNode.BranchTrue(Symbol.Take(condition, typeof(bool)), branchBeginLabel));
                Ctx._EmitList = branchBody;
                _Branches.Add(new Branch(branchBeginLabel, branchBody));
            }

            public IBlock Else()
            {
                if (_Continued)
                    throw new InvalidOperationException("If statement may only have one fallback else statement");
                _Continued = true;
                Ctx._EmitList = _OuterEmitList;
                return new ElseBlock(Ctx, EnsureLevel(), _Branches, _EscapeLabel);
            }

            public override void End()
            {
                base.End();
                if (_Continued)
                    throw new InvalidOperationException("This if statement is continued to an open else block. end the statement on the else block");
                Ctx._EmitList = _OuterEmitList;
                Ctx._Target.Put(ILExpressionNode.Branch(_EscapeLabel));
                foreach (var branch in _Branches)
                    branch.Write(Ctx._Target, _EscapeLabel);
                Ctx._Target.Put(ILExpressionNode.Label(_EscapeLabel));
            }
        }

        private class ElseBlock : Block
        {
            public ElseBlock(MethodGeneratorContext ctx, int level, List<Branch> branches, Label escapeLabel) : base(ctx, level)
            {
                _Branches = branches;
                _EscapeLabel = escapeLabel;
            }

            private List<Branch> _Branches;
            private Label _EscapeLabel;

            public override void End()
            {
                base.End();
                Ctx._Target.Put(ILExpressionNode.Branch(_EscapeLabel));
                foreach (var branch in _Branches)
                    branch.Write(Ctx._Target, _EscapeLabel);
                Ctx._Target.Put(ILExpressionNode.Label(_EscapeLabel));
            }
        }

        private struct Branch
        {
            public Label _BeginLabel;
            public List<IEmittable> _EmitList;

            public Branch(Label beginLabel, List<IEmittable> emitList)
            {
                _BeginLabel = beginLabel;
                _EmitList = emitList;
            }

            public void Write(GeneratorTarget target, Label escapeLabel)
            {
                target.Put(ILExpressionNode.Label(_BeginLabel));
                target.Put(_EmitList);
                target.Put(ILExpressionNode.Branch(escapeLabel));
            }
        }
    }
}
