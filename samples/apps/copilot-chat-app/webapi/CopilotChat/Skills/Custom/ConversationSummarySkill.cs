// Copyright (c) Microsoft. All rights reserved.

using System.Collections.Generic;
using System.Threading.Tasks;

using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Orchestration;
using Microsoft.SemanticKernel.SkillDefinition;
using Microsoft.SemanticKernel.Text;

namespace SemanticKernel.Service.CopilotChat.Skills.Custom;

/// <summary>
/// <para>Semantic skill that enables conversations summarization.</para>
/// </summary>
/// <example>
/// <code>
/// var kernel Kernel.Builder.Build();
/// kernel.ImportSkill(new SummarySkill(kernel));
/// </code>
/// </example>
public class SummarySkill
{
    /// <summary>
    /// The max tokens to process in a single semantic function call.
    /// </summary>
    private const int MaxTokens = 1024;

    private readonly ISKFunction _summarizeConversationFunction;

    /// <summary>
    /// Initializes a new instance of the <see cref="SummarySkill"/> class.
    /// </summary>
    /// <param name="kernel">Kernel instance</param>
    public SummarySkill(IKernel kernel)
    {
        this._summarizeConversationFunction = kernel.CreateSemanticFunction(
            SummarizeConversationDefinition,
            skillName: nameof(SummarySkill),
            description: "Given a section of a conversation transcript, summarize the part of the conversation.",
            maxTokens: MaxTokens,
            temperature: 0.1,
            topP: 0.5);
    }

    /// <summary>
    /// Given a long conversation transcript, summarize the conversation.
    /// </summary>
    /// <param name="input">A long conversation transcript.</param>
    /// <param name="context">The SKContext for function execution.</param>
    [SKFunction("Given a long conversation transcript, summarize the conversation.")]
    [SKFunctionName("SummarizeConversation")]
    [SKFunctionInput(Description = "A long conversation transcript.")]
    public Task<SKContext> SummarizeConversationAsync(string input, SKContext context)
    {
        List<string> lines = TextChunker.SplitPlainTextLines(input, MaxTokens);
        List<string> paragraphs = TextChunker.SplitPlainTextParagraphs(lines, MaxTokens);

        return this._summarizeConversationFunction
            .AggregatePartitionedResultsAsync(paragraphs, context);
    }

    private const string SummarizeConversationDefinition =
        @"BEGIN CONTENT TO SUMMARIZE:
{{$INPUT}}

END CONTENT TO SUMMARIZE.

Summarize the conversation in 'CONTENT TO SUMMARIZE', identifying main points of discussion and any conclusions that were reached.
Do not incorporate other general knowledge.
Summary is in plain text, in complete sentences, with no markup or tags.
Use HEBREW for the summary.

BEGIN SUMMARY:
";

}
