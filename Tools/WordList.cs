using System.Text;

namespace WordGame.Tools
{
    public class WordList
    {
        private readonly List<string> _words;
        public WordList()
        {
            var filePath = Path.Combine("szavak.txt");
            if (File.Exists(filePath))
            {
                _words = File.ReadAllLines(filePath, Encoding.UTF8)
                             .Where(w => !string.IsNullOrWhiteSpace(w))
                             .Select(w => w.Trim().ToLower())
                             .ToList();
            }
            else
            {
                _words = new List<string>();
            }
        }
        public IReadOnlyList<string> GetWords() => _words;
    }
}
