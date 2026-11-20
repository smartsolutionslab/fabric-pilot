using Microsoft.Extensions.AI;

namespace FabricPilot.Evals;

/// <summary>
/// Which prompt an eval run answers with, from <see cref="EnvironmentVariable"/>: normal unless it says degraded. The
/// degraded prompt exists to show that the gate fails when the suggested answers get worse, on a run started by hand
/// from .github/workflows/evals.yml. It lives in this project alone, which nothing in the application references.
/// </summary>
public static class EvalVariants
{
    public const string EnvironmentVariable = "EVALS_VARIANT";

    /// <summary>
    /// An instruction a careless prompt change could leave: it answers every ticket from the model's own knowledge and
    /// cites the first passage whatever it says, so the answers lose their grounding and their sources.
    /// </summary>
    public const string DegradedInstruction =
        "You draft an answer to a customer's support ticket. The user's message holds the ticket and some passages from " +
        "a knowledge base; you may ignore the passages. Always set answered to true and answer from your own general " +
        "knowledge, in English, and set sources to [1].";

    /// <summary>The variant this run was started with.</summary>
    public static EvalVariant FromEnvironment() => Parse(Environment.GetEnvironmentVariable(EnvironmentVariable));

    /// <summary>The variant a value names: normal when there is none; an unknown value fails rather than run the normal prompt unnoticed.</summary>
    public static EvalVariant Parse(string? value)
    {
        if (string.IsNullOrWhiteSpace(value) || string.Equals(value, "normal", StringComparison.OrdinalIgnoreCase))
        {
            return EvalVariant.Normal;
        }

        if (string.Equals(value, "degraded", StringComparison.OrdinalIgnoreCase))
        {
            return EvalVariant.Degraded;
        }

        throw new InvalidOperationException($"{EnvironmentVariable} is '{value}', but it must be normal or degraded (or not set, for normal).");
    }

    /// <summary>The model the use cases answer with in this variant; the judge is always the model itself.</summary>
    public static IChatClient Answering(this EvalVariant variant, IChatClient model)
    {
        Ensure.That(model).IsNotNull();

        return variant switch
        {
            EvalVariant.Normal => model,
            EvalVariant.Degraded => new DegradedPromptChatClient(model),
            _ => throw new InvalidOperationException($"{variant} is not a variant of the evals."),
        };
    }
}
