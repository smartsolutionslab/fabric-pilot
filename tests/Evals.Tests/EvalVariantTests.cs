using FabricPilot.Modules.Knowledge.Application;
using FabricPilot.Modules.Knowledge.Application.Queries.SuggestAnswer;
using FabricPilot.Modules.Knowledge.Domain.Articles;
using FabricPilot.Tests.Fakes;
using Microsoft.Extensions.AI;

namespace FabricPilot.Evals.Tests;

/// <summary>
/// The variant the evals answer with: the suggested answers' own prompt, or, chosen by hand for one run, a degraded one
/// that shows the gate failing. The degraded prompt lives in the eval project alone; the module's handler never sees it
/// outside an eval run.
/// </summary>
public sealed class EvalVariantTests
{
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("normal")]
    public void Without_a_variant_or_with_normal_the_evals_run_the_normal_prompt(string? value)
    {
        EvalVariants.Parse(value).ShouldBe(EvalVariant.Normal);
    }

    [Theory]
    [InlineData("degraded")]
    [InlineData("Degraded")]
    public void Degraded_selects_the_degraded_prompt(string value)
    {
        EvalVariants.Parse(value).ShouldBe(EvalVariant.Degraded);
    }

    [Fact]
    public void An_unknown_variant_fails_naming_the_variable_and_the_values_it_takes_rather_than_running_the_normal_prompt()
    {
        var message = Should.Throw<InvalidOperationException>(() => EvalVariants.Parse("worse")).Message;

        message.ShouldContain(EvalVariants.EnvironmentVariable);
        message.ShouldContain("'worse'");
        message.ShouldContain("normal");
        message.ShouldContain("degraded");
    }

    [Fact]
    public void The_variable_is_EVALS_VARIANT()
    {
        EvalVariants.EnvironmentVariable.ShouldBe("EVALS_VARIANT");
    }

    [Fact]
    public async Task The_normal_variant_answers_with_the_model_as_it_is_so_the_handler_sends_its_own_instruction()
    {
        using var model = new FakeChatClient("{\"answered\":false,\"answer\":\"\",\"sources\":[]}");

        var answering = EvalVariant.Normal.Answering(model);
        await Ask(answering);

        answering.ShouldBeSameAs(model);
        model.Requests.ShouldHaveSingleItem().First(message => message.Role == ChatRole.System).Text.ShouldBe(SuggestionPrompt.Instruction);
    }

    [Fact]
    public async Task The_degraded_variant_replaces_the_suggested_answers_instruction_and_leaves_the_ticket_and_passages_as_they_are()
    {
        using var model = new FakeChatClient("{\"answered\":false,\"answer\":\"\",\"sources\":[]}");

        await Ask(EvalVariant.Degraded.Answering(model));

        var request = model.Requests.ShouldHaveSingleItem();
        request.First(message => message.Role == ChatRole.System).Text.ShouldBe(EvalVariants.DegradedInstruction);
        EvalVariants.DegradedInstruction.ShouldNotBe(SuggestionPrompt.Instruction);
        request.Last(message => message.Role == ChatRole.User).Text.ShouldContain("<passage number=\"1\">\nVPN sign-in fails");
    }

    [Fact]
    public async Task The_degraded_variant_leaves_every_other_use_cases_messages_alone()
    {
        using var model = new FakeChatClient("An answer.");
        var messages = EvalPrompt.Messages(new EvalExample("When is invoice 7 due?", "Invoice 7 is due on 1 May 2026."));

        await EvalVariant.Degraded.Answering(model).GetResponseAsync(messages, cancellationToken: TestContext.Current.CancellationToken);

        model.Requests.ShouldHaveSingleItem().Select(message => message.Text).ShouldBe(messages.Select(message => message.Text));
    }

    private static Task<Result<Modules.Knowledge.Application.DTOs.SuggestedAnswerDto, ApiError>> Ask(IChatClient model) =>
        new SuggestAnswerHandler(model, new OnePassage()).HandleAsync(new SuggestAnswer("VPN sign-in rejected"), TestContext.Current.CancellationToken);

    private sealed class OnePassage : IArticlePassages
    {
        public Task<IReadOnlyList<ArticlePassage>> NearestAsync(string question, int count, CancellationToken cancellationToken) =>
            Task.FromResult<IReadOnlyList<ArticlePassage>>([new ArticlePassage(ArticleId.New(), "VPN sign-in fails", "VPN sign-in fails\n\nReset the password.")]);
    }
}
