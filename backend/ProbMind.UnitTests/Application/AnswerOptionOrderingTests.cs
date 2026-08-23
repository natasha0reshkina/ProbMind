using ProbMind.Application.Common;
using ProbMind.Domain.Entities;

namespace ProbMind.UnitTests.Application;

public sealed class AnswerOptionOrderingTests
{
    [Fact]
    public void ForSession_KeepsOrderStableWithinSessionAndChangesAcrossSessions()
    {
        var questionId = new Guid("00000000-0000-0000-0000-000000000064");
        var options = Enumerable.Range(1, 4)
            .Select(index => new AnswerOption
            {
                Id = new Guid($"00000000-0000-0000-0000-{index:000000000000}"),
                Text = $"Вариант {index}",
                SortOrder = index,
                IsCorrect = index == 1
            })
            .ToArray();

        var firstSession = new Guid("00000000-0000-0000-0000-000000000010");
        var secondSession = new Guid("00000000-0000-0000-0000-000000000011");

        var first = AnswerOptionOrdering.ForSession(options, firstSession, questionId);
        var repeated = AnswerOptionOrdering.ForSession(options, firstSession, questionId);
        var second = AnswerOptionOrdering.ForSession(options, secondSession, questionId);

        Assert.True(first.Select(x => x.Id).SequenceEqual(repeated.Select(x => x.Id)));
        Assert.False(first.Select(x => x.Id).SequenceEqual(second.Select(x => x.Id)));
        Assert.True(new[] { 1, 2, 3, 4 }.SequenceEqual(first.Select(x => x.SortOrder)));
    }
}
