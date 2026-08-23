using System.Security.Cryptography;
using System.Text;
using ProbMind.Application.Contracts;
using ProbMind.Domain.Entities;

namespace ProbMind.Application.Common;

public static class AnswerOptionOrdering
{
    public static AnswerOptionDto[] ForSession(
        IEnumerable<AnswerOption> options,
        Guid sessionId,
        Guid questionId)
    {
        return options
            .OrderBy(option => OrderKey(sessionId, questionId, option.Id), StringComparer.Ordinal)
            .ThenBy(option => option.Id)
            .Select((option, index) => new AnswerOptionDto(option.Id, option.Text, index + 1))
            .ToArray();
    }

    private static string OrderKey(Guid sessionId, Guid questionId, Guid optionId)
    {
        var value = $"{sessionId:N}:{questionId:N}:{optionId:N}";
        return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(value)));
    }
}
