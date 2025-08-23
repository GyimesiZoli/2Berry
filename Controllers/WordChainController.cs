using System.Globalization;
using System.Text;
using WordGame.Models;
using WordGame.Tools;
using Microsoft.AspNetCore.Mvc;

namespace WordGame.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class WordChainController : ControllerBase
    {
        private readonly WordList _wordService;
        private readonly char[] _alphabet;

        public WordChainController(WordList wordService)
        {
            _wordService = wordService;

            string NormHu(string s) =>
                s.ToLower(new CultureInfo("hu-HU")).Normalize(NormalizationForm.FormC);

            var letters = new HashSet<char>();
            foreach (var w in _wordService.GetWords().Where(x => x.Length == 5).Select(NormHu))
                foreach (var ch in w)
                    letters.Add(ch);

            _alphabet = letters.ToArray();
        }

        [HttpPost]
        public IActionResult GetWordChain([FromBody] WordChainRequest request)
        {
            string NormHu(string s) =>
                s.ToLower(new CultureInfo("hu-HU")).Normalize(NormalizationForm.FormC);

            var startWord = NormHu(request.Source?.Trim() ?? "");
            if (string.IsNullOrEmpty(startWord)) return BadRequest("A kezdőszó üres, kérek helyette egy 5 betűs szót!");
            if (startWord.Length != 5) return BadRequest($"A kezdőszó {startWord.Length} betűből áll, kérek helyette egy 5 betűst!");

            var endWord = NormHu(request.Target?.Trim() ?? "");
            if (string.IsNullOrEmpty(endWord)) return BadRequest("A végszó üres, kérek helyette egy 5 betűs szót!");
            if (endWord.Length != 5) return BadRequest($"A végszó {endWord.Length} betűből áll, kérek helyette egy 5 betűst!");

            var words = _wordService.GetWords();
            var wordSet = new HashSet<string>(words.Where(w => w.Length == 5).Select(NormHu));

            var path = FindWordChainAStar(startWord, endWord, wordSet);

            if (path == null || path.Count == 0)
                return NotFound("Sajnos nem tudok láncot alkotni a két szó között :(");

            return Ok(path);
        }

        private List<string>? FindWordChainAStar(string start, string end, HashSet<string> wordSet)
        {
            if (!wordSet.Contains(start) || !wordSet.Contains(end))
                return null;

            var cameFrom = new Dictionary<string, string?>();
            var gScore = new Dictionary<string, int>();
            foreach (var w in wordSet) gScore[w] = int.MaxValue;
            gScore[start] = 0;

            var open = new PriorityQueue<string, int>();
            open.Enqueue(start, Heuristic(start, end));
            var closed = new HashSet<string>();

            while (open.Count > 0)
            {
                var current = open.Dequeue();
                if (current == end) return ReconstructPath(cameFrom, current);

                closed.Add(current);

                foreach (var neighbor in GetNeighbors(current, wordSet))
                {
                    if (closed.Contains(neighbor)) continue;

                    int tentativeG = gScore[current] + 1;
                    if (tentativeG < gScore[neighbor])
                    {
                        cameFrom[neighbor] = current;
                        gScore[neighbor] = tentativeG;
                        int fScore = tentativeG + Heuristic(neighbor, end);
                        open.Enqueue(neighbor, fScore);
                    }
                }
            }

            return null;
        }

        private int Heuristic(string word, string target)
        {
            int diff = 0;
            for (int i = 0; i < word.Length; i++)
                if (word[i] != target[i]) diff++;
            return diff;
        }

        private List<string> ReconstructPath(Dictionary<string, string?> cameFrom, string current)
        {
            var path = new List<string> { current };
            while (cameFrom.ContainsKey(current))
            {
                current = cameFrom[current]!;
                if (current != null) path.Insert(0, current);
            }
            return path;
        }

        private IEnumerable<string> GetNeighbors(string word, HashSet<string> wordSet)
        {
            var chars = word.ToCharArray();
            for (int i = 0; i < chars.Length; i++)
            {
                var original = chars[i];
                foreach (var c in _alphabet)
                {
                    if (c == original) continue;
                    chars[i] = c;
                    var cand = new string(chars);
                    if (wordSet.Contains(cand))
                        yield return cand;
                }
                chars[i] = original;
            }
        }
    }
}
