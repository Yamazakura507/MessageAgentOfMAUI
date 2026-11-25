namespace MessageAgent.Core.Models.Dialogs
{
    /// <summary>
    /// A selectable question-dialog button and its returned value.
    /// </summary>
    public sealed record MessageQuestionButton(string Text, string Value, bool IsPrimary = false, bool IsDestructive = false);
}
