using System.Text.Json;
using System.Text.Json.Serialization;

namespace elixir_app;

public class DAL
{
    private Dictionary<string, List<Answer>> DB = new Dictionary<string, List<Answer>>();

    public List<Answer> GetUserAnswers(string name)
    {
        if (this.DB.ContainsKey(name))
            return this.DB[name];
        return new List<Answer>();
    }

    public void ClearUserAnswers(string name)
    {
        if (this.DB.ContainsKey(name))
            this.DB[name].Clear();
    }
    
    public void AddUserAnswers(string name, List<Answer> answers)
    {
        ClearUserAnswers(name);
        foreach (var a in answers)
            AddUserAnswer(name, a.QuestionId, a.AnswerText);
    }

    public Answer? AddUserAnswer(string name, int questionId, string answerText)
    {
        if (!this.DB.ContainsKey(name))
            this.DB.Add(name, new List<Answer>());
        
        var answers = this.DB[name];
        var answer = answers.FirstOrDefault(q => q.QuestionId == questionId);
        if (answer == null)
        {
            answer = new Answer(questionId, answerText);
            this.DB[name].Add(answer);
        }
        answer.AnswerText = answerText;
        return answer;
    }

    public void SaveDbFileOrThrow(string filepath)
    {
        var json = JsonSerializer.Serialize(this.DB);
        File.WriteAllText(filepath, json);
    }
    
    // Loading a non-existent file is a NOOP on purpose so the calling code doesn't have know if it's the first time
    // running the program.
    //
    public void LoadDbFileOrThrow(string filepath)
    {
        if (!File.Exists(filepath))
            return;
        var json = File.ReadAllText(filepath);
        this.DB = JsonSerializer.Deserialize<Dictionary<string, List<Answer>>>(json) ?? this.DB;
    }
}
