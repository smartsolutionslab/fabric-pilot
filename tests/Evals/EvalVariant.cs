namespace FabricPilot.Evals;

/// <summary>The prompt the evals answer with; see <see cref="EvalVariants"/>.</summary>
public enum EvalVariant
{
    /// <summary>The use cases' own prompts, as the application sends them.</summary>
    Normal,

    /// <summary>The suggested answers with <see cref="EvalVariants.DegradedInstruction"/>, to show the gate failing.</summary>
    Degraded,
}
