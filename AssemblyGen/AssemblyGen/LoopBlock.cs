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
        private class LoopBlock : Block, ILoopBlock
        {
            public LoopBlock(MethodGeneratorContext ctx, int level, Label repeatLabel) : base(ctx, level)
            {
                _RepeatLabel = repeatLabel;
                _EscapeLabel = ctx._Il.DefineLabel();
            }

            private Label _RepeatLabel;
            private Label _EscapeLabel;

            public void Break()
            {
                if (HasEnded)
                    throw new InvalidOperationException("The loop has already ended");
                Ctx._Target.Put(ILExpressionNode.Branch(_EscapeLabel));
            }


            public void Continue()
            {
                if (HasEnded)
                    throw new InvalidOperationException($"The loop has already ended");
                Ctx._Target.Put(ILExpressionNode.Branch(_RepeatLabel));
            }

            public override void End()
            {
                base.End();
                Ctx._Target.Put(ILExpressionNode.Branch(_RepeatLabel));
                Ctx._Target.Put(ILExpressionNode.Label(_EscapeLabel));
            }
        }
    }
}
