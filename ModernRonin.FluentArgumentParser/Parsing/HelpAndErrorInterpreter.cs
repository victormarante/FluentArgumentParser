using System;
using System.Collections.Generic;
using System.Linq;

using ModernRonin.FluentArgumentParser.Definition;
using ModernRonin.FluentArgumentParser.Help;

namespace ModernRonin.FluentArgumentParser.Parsing;

public class HelpAndErrorInterpreter : IHelpAndErrorInterpreter
{
    readonly IHelpMaker _helpMaker;

    public HelpAndErrorInterpreter(IHelpMaker helpMaker) => _helpMaker = helpMaker;

    public HelpResult Interpret(VerbCall call, ICommandLineParser parser)
    {
        if (call.IsHelpRequest) return MakeHelpResult(call, parser);
        return call.HasError ? ForBadArguments(call, parser.Configuration) : default;
    }

    public string GetHelpOverview(ICommandLineParser parser) => _helpMaker.GenerateFor(parser);

    HelpResult ForBadArguments(VerbCall call, ParserConfiguration configuration)
    {
        var argumentErrors = string.Join(Environment.NewLine, call.Arguments.Select(a => a.Error));
        return new HelpResult
        {
            IsResultOfInvalidInput = true,
            Text = HelpFor(call, configuration) +
                   Environment.NewLine +
                   argumentErrors
        };
    }

    string HelpFor(VerbCall call, ParserConfiguration configuration) =>
        _helpMaker.GenerateFor(call.Verb, call.IsDefaultVerb, configuration);

    HelpResult MakeHelpResult(VerbCall call, ICommandLineParser parser)
    {
        if (call.Verb == default)
        {
            if (call.UnknownVerb == default) return new HelpResult { Text = _helpMaker.GenerateFor(parser) };
            return new HelpResult
            {
                IsResultOfInvalidInput = true,
                Text = _helpMaker.GenerateFor(parser) + Environment.NewLine + createSuggestionText(),
            };
        }

        if (call.UnknownVerb != default)
        {
            return new HelpResult
            {
                IsResultOfInvalidInput = true,
                Text = HelpFor(call, parser.Configuration) + Environment.NewLine + createSuggestionText(),
            };
        }

        string createSuggestionText()
        {
            var suggesstions = GetVerbSuggestions(call.UnknownVerb, parser.Verbs.Select(x => x.Name).ToArray());
            if (!suggesstions.Any())
            {
                return $"Unknown verb '{call.UnknownVerb}'";
            }

            return $"Unknown verb '{call.UnknownVerb}'. Did you mean: {string.Join(", ", suggesstions)}";
        }

        return new HelpResult { Text = HelpFor(call, parser.Configuration) };
    }

    /// <summary>
    ///     <para>
    ///     GetVerbSuggestions calculates similar verb suggestions based on user input
    ///     </para>
    ///     Levensthein distance algorithm is used to determine to number of edits required to match a known verb.
    ///     If only 3 or less edits are required, it is considered a good guess as to what the user intended to type.
    /// </summary>
    private List<string> GetVerbSuggestions(string unknownVerb, string[] knownVerbs)
    {
        var suggestions = new List<string>();
        foreach(var verb in knownVerbs)
        {
            var distance = CalculateLevenstheinDistance(unknownVerb, verb);
            if (distance <= 3) // TODO: How do we configure this?
            {
                suggestions.Add(verb);
            }
        }

        return suggestions;
    }

    private int CalculateLevenstheinDistance(string source, string target)
    {
        if (target.Length == 0) return source.Length;
        if (source.Length == 0) return target.Length;

        if (source[0] == target[0])
        {
            return CalculateLevenstheinDistance(source[1..], source[1..]);
        }

        return new[]
        {
            CalculateLevenstheinDistance(source[1..], target),
            CalculateLevenstheinDistance(source, target[1..]),
            CalculateLevenstheinDistance(source[1..], target[1..]),
        }.Min() + 1;
    }
}