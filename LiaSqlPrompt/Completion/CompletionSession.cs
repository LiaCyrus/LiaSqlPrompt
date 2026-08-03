namespace LiaSqlPrompt.Completion
{
    public sealed class CompletionSession
    {
        public int Start { get; }
        public int Length { get; }

        public CompletionSession(int start, int length)
        {
            Start = start;
            Length = length;
        }
    }
}