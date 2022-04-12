using System.Globalization;
using System.Text.Json;

namespace elixir_app;

public class Question
{
    public int QuestionId { get; }
    public string QuestionText { get; } = "";

    public Question(int questionId, string questionText)
    {
        this.QuestionId = questionId;
        if (string.IsNullOrWhiteSpace(questionText))
            throw new ArgumentException("question cannot be null or whitespace.", nameof(questionText));
        this.QuestionText = questionText;
    }
}
