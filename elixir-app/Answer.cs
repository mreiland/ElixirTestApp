namespace elixir_app;

public class Answer
{
    public int QuestionId { get; private set; }
    public string AnswerText { get; set; } = "";

    public Answer(int questionId, string answerText)
    {
        if (string.IsNullOrWhiteSpace(answerText))
            throw new ArgumentException("answer text cannot be null or whitespace.", nameof(answerText));
        this.QuestionId = questionId;
        this.AnswerText = answerText;
    }
}
