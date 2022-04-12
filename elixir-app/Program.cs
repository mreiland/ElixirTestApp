// See https://aka.ms/new-console-template for more information

using elixir_app;

string DBFilename = "elixir.db";
string QuestionsFilename = "questions.txt";
Run(DBFilename, QuestionsFilename);


void Run(string DBFilename, string QuestionsFilename)
{
    string dbPath = GetDbFilePath(DBFilename);
    var dal = new DAL();
    dal.LoadDbFileOrThrow(dbPath);

    string questionPath = GetQuestionsFilePath(QuestionsFilename);
    var questions = LoadQuestionsFileOrThrow(questionPath);
    
    var next = NextAction.PromptForName;
    var name = "";
    while (next != NextAction.Shutdown && next != NextAction.None)
    {
        switch (next)
        {
            case NextAction.PromptForName:
                var tup = FLOW_PromptForName(dal);
                if (!string.IsNullOrWhiteSpace(tup.Name))
                    name = tup.Name;
                next = tup.NextAction;
                break;
            case NextAction.Answer:
                next = FLOW_Answer(name, questions, dal); break;
            case NextAction.Store:
                next = FLOW_Store(name, questions, dal);
                dal.SaveDbFileOrThrow(dbPath);
                break;
            case NextAction.Shutdown:
                return;
            default:
                throw new ArgumentException($"NextAction value of '{Enum.GetName(next)}' is unrecognized.");
        }
    }
}
    
// NOTE:  We need a stable id across loads.  Since I don't have control over the input file we use the
//        index as the id.  Or perhaps that was the point of not specifying the input file for the questions?
//        Unsure, but I've chosen to take the conservative route and keep it as simple as possible.
//
// NOTE:  This approach of using the index as the id has the downside of doing the wrong thing if someone
//        moves questions around so this has an assumption that won't happen.
//
List<Question> LoadQuestionsFileOrThrow(string filepath)
{
    if (!File.Exists(filepath))
        throw new Exception($"The file {filepath} does not exist");
    var idx = 0;
    return File.ReadAllLines(filepath).Select(l=>new Question(questionId: idx++, questionText: l.Trim())).ToList();
}

(string Name, NextAction NextAction) FLOW_PromptForName(DAL dal)
{
    var nameResp = PromptForString("Hi, what is your name?");
    if (!nameResp.HasInput)
    {
        DisplayMessage("Sorry, we could not get your name.");
        return ("", NextAction.Shutdown);
    }

    var name = nameResp.Input.Trim();
    var answers = dal.GetUserAnswers(name);
    if (!answers.Any())
        return (name, NextAction.Store);

    var securityResp = PromptForYN("Do you want to answer a security question?");
    if (!securityResp.HasInput)
    {
        DisplayMessage("Sorry, your input wasn't understood.");
        return (name, NextAction.Shutdown);
    }

    return securityResp.IsYes
        ? (name, NextAction.Answer)
        : (name, NextAction.Store);
}

NextAction FLOW_Store(string name, List<Question> questions, DAL dal)
{
    var resp = PromptForYN("Would you like to store answers to security questions?");
    if (!resp.HasInput)
    {
        DisplayMessage("Sorry, your input wasn't understood.");
        return NextAction.Shutdown;
    }
    if(resp.IsNo)
        return NextAction.PromptForName;

    // We don't write to the DAL until we know the user has successfully answered
    // enough questions.
    //
    var done = false;
    var answers = new List<Answer>();
    while (!done)
    {
        answers.Clear();
        foreach (var q in questions)
        {
            var tup = PromptForString(q.QuestionText, repeatOnEmpty: false);
            if (!tup.HasInput || string.IsNullOrWhiteSpace(tup.Input))
                continue;
            answers.Add(new Answer(q.QuestionId, tup.Input));
        }

        done = answers.Count >= 3;
        if (!done)
        {
            DisplayMessage("Users are required to answer at least 3 questions, starting over.");
            continue;
        }
        dal.ClearUserAnswers(name);
        dal.AddUserAnswers(name, answers);
    }
    return NextAction.PromptForName;
}

NextAction FLOW_Answer(string name, List<Question> questions, DAL dal)
{
    var questionsById = questions.ToDictionary(q => q.QuestionId);
    var answers = dal.GetUserAnswers(name);
    foreach (var answer in answers)
    {
        // if the question has been removed just move along
        //
        if (!questionsById.ContainsKey(answer.QuestionId))
            continue;
        var question = questionsById[answer.QuestionId];
        var tup = PromptForString(question.QuestionText, repeatOnEmpty: false);
        if(!tup.HasInput)
            continue;
        if (tup.Input.Trim().Equals(answer.AnswerText, StringComparison.OrdinalIgnoreCase))
        {
            DisplayMessage("That answer is correct!");
            return NextAction.PromptForName;
        }
    }
    DisplayMessage("You have not answered any of the security questions correctly.");
    return NextAction.PromptForName;
}

void DisplayMessage(string msg)
{
    Console.WriteLine(msg);
}

string GetDbFilePath(string dbFileName)
{
    return Path.Join(GetExePath(), dbFileName);
}

string GetQuestionsFilePath(string questionsFilename)
{
    return Path.Join(GetExePath(), questionsFilename);
}

string GetExePath()
{
    return Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location) ?? "";
}

(string? Input, bool HasInput) PromptForString(string displayMessage, bool repeatOnEmpty = true)
{
    var input = "";
    var maxTries = 10;
    var tries = 0;

    while (tries < maxTries)
    {
        tries++;
        Console.WriteLine(displayMessage);
        input = Console.ReadLine();
        if (string.IsNullOrWhiteSpace(input) && repeatOnEmpty )
            continue;
        return (Input: input?.Trim() ?? "", HasInput: !string.IsNullOrWhiteSpace(input));
    }
    return (Input: "", HasInput: false);
}

(string? Input, bool HasInput, bool IsYes, bool IsNo) PromptForYN(string displayMessage, bool repeatOnEmpty = true)
{
    var input = "";
    var maxTries = 10;
    var tries = 0;
    var yes = new[] {"y", "yes"};
    var no = new[] {"n", "no"};
    var validResponse = false;
    
    while (!validResponse && tries < maxTries)
    {
        tries++;
        
        Console.WriteLine(displayMessage);
        input = Console.ReadLine();
        if (string.IsNullOrWhiteSpace(input))
            continue;
        input = input.Trim();
        var isYes = yes.Any(r => r.Equals(input, StringComparison.OrdinalIgnoreCase));
        var isNo = no.Any(r => r.Equals(input, StringComparison.OrdinalIgnoreCase));
        validResponse = isYes || isNo;

        if (isYes || isNo)
            return (Input: input, HasInput: true, IsYes: isYes, IsNo: isNo);
    }
    return (Input: "", HasInput: false, IsYes: false, IsNo: false);
}
