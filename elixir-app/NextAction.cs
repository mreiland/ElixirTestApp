using System.Text.Json;
using System.Text.Json.Serialization;

namespace elixir_app;

public enum NextAction
{
    None = 0,
    PromptForName,
    Store,
    Answer,
    Shutdown
}
