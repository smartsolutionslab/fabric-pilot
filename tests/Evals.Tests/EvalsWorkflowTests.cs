using YamlDotNet.RepresentationModel;

namespace FabricPilot.Evals.Tests;

/// <summary>
/// The workflow that runs the evals against the project's Azure OpenAI deployment: by hand, in either variant, or
/// weekly when the repository variable says so; never on a push or a pull request, whose checks must not call a model.
/// </summary>
public sealed class EvalsWorkflowTests
{
    private static readonly YamlMappingNode Workflow = Load();

    [Fact]
    public void It_runs_by_hand_or_on_its_schedule_and_on_nothing_else()
    {
        var triggers = Map(Workflow, "on").Children.Keys.Select(key => ((YamlScalarNode)key).Value).ToList();

        triggers.ShouldBe(["workflow_dispatch", "schedule"], ignoreOrder: true);
        Scalar(Map(Workflow, "jobs", "evals"), "if").ShouldContain("vars.EVALS_SCHEDULED == 'true'");
    }

    [Fact]
    public void A_run_by_hand_chooses_the_variant_normal_by_default_or_degraded()
    {
        var variant = Map(Workflow, "on", "workflow_dispatch", "inputs", "variant");

        Scalar(variant, "type").ShouldBe("choice");
        Scalar(variant, "default").ShouldBe("normal");
        ((YamlSequenceNode)variant.Children[new YamlScalarNode("options")]).Children.Select(option => ((YamlScalarNode)option).Value).ShouldBe(["normal", "degraded"]);
    }

    [Fact]
    public void The_chosen_variant_reaches_the_evals_and_a_scheduled_run_is_normal()
    {
        var environment = Map(Workflow, "jobs", "evals", "env");

        Scalar(environment, EvalVariants.EnvironmentVariable).ShouldBe("${{ inputs.variant || 'normal' }}");
    }

    [Fact]
    public void The_model_is_the_repositorys_Azure_OpenAI_deployment_with_its_key_from_the_repository_secret()
    {
        var environment = Map(Workflow, "jobs", "evals", "env");

        Scalar(environment, "Parameters__azure-openai-api-key").ShouldBe("${{ secrets.AZURE_OPENAI_API_KEY }}");
        Scalar(environment, "Parameters__azure-openai-endpoint").ShouldBe("${{ vars.AZURE_OPENAI_ENDPOINT }}");
        Scalar(environment, "Parameters__azure-openai-deployment").ShouldBe("${{ vars.AZURE_OPENAI_DEPLOYMENT }}");
    }

    private static YamlMappingNode Load()
    {
        var root = Path.GetDirectoryName(EvalPaths.FabricYaml)!;
        var stream = new YamlStream();
        using var reader = new StreamReader(Path.Combine(root, ".github", "workflows", "evals.yml"));
        stream.Load(reader);
        return (YamlMappingNode)stream.Documents[0].RootNode;
    }

    private static YamlMappingNode Map(YamlMappingNode node, params string[] path) =>
        path.Aggregate(node, (current, key) => (YamlMappingNode)current.Children[new YamlScalarNode(key)]);

    private static string? Scalar(YamlMappingNode node, string key) =>
        ((YamlScalarNode)node.Children[new YamlScalarNode(key)]).Value;
}
