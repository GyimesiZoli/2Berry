using System.Globalization;
using System.Text;
using System.Linq;
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
        private static readonly CultureInfo Hu = new("hu-HU");
        private static string NormHu(string s) =>
            (s ?? string.Empty).ToLower(Hu).Normalize(NormalizationForm.FormC);
        private static volatile bool _cacheBuilt = false;
        private static readonly object _cacheLock = new();
        private static char[] _alphabet = Array.Empty<char>();
        private static HashSet<string> _wordSet = new();

        public WordChainController(WordList wordService)
        {
            _wordService = wordService;

            if (!_cacheBuilt)
            {
                lock (_cacheLock)
                {
                    if (!_cacheBuilt)
                    {
                        var words = _wordService.GetWords();
                        _wordSet = new HashSet<string>(
                            words.Where(w => w?.Length == 5).Select(NormHu)
                        );

                        var letters = new HashSet<char>();
                        foreach (var w in _wordSet)
                            foreach (var ch in w)
                                letters.Add(ch);

                        _alphabet = letters.ToArray();

                        _cacheBuilt = true;
                    }
                }
            }
        }

        [HttpPost]
        public IActionResult GetWordChain([FromBody] WordChainRequest request)
        {
            var startWord = NormHu(request?.Source?.Trim());
            if (string.IsNullOrEmpty(startWord))
                return BadRequest("A kezdőszó üres, kérek helyette egy 5 betűs szót!");
            if (startWord.Length != 5)
                return BadRequest($"A kezdőszó {startWord.Length} betűből áll, kérek helyette egy 5 betűst!");

            var endWord = NormHu(request?.Target?.Trim());
            if (string.IsNullOrEmpty(endWord))
                return BadRequest("A végszó üres, kérek helyette egy 5 betűs szót!");
            if (endWord.Length != 5)
                return BadRequest($"A végszó {endWord.Length} betűből áll, kérek helyette egy 5 betűst!");

            var path = FindWordChainAStar(startWord, endWord);
            if (path == null || path.Count == 0)
                return NotFound("Sajnos nem tudok láncot alkotni a két szó között :(");

            return Ok(path);
        }

        private List<string>? FindWordChainAStar(string start, string end)
        {
            if (start.Length != end.Length) return null;
            if (start == end) return new List<string> { start };
            if (!_wordSet.Contains(start) || !_wordSet.Contains(end)) return null;

            var cameFrom = new Dictionary<string, string>();
            var gScore = new Dictionary<string, int>();

            foreach (var w in _wordSet) gScore[w] = int.MaxValue;
            gScore[start] = 0;

            var open = new PriorityQueue<string, int>();
            open.Enqueue(start, Heuristic(start, end));

            var closed = new HashSet<string>();

            while (open.Count > 0)
            {
                var current = open.Dequeue();

                if (closed.Contains(current)) continue;

                if (current == end)
                    return ReconstructPath(cameFrom, current);

                closed.Add(current);

                foreach (var neighbor in GetNeighbors(current))
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

        private static int Heuristic(string word, string target)
        {
            int diff = 0;
            for (int i = 0; i < word.Length; i++)
                if (word[i] != target[i]) diff++;
            return diff;
        }

        private static List<string> ReconstructPath(Dictionary<string, string> cameFrom, string current)
        {
            var path = new List<string> { current };
            while (cameFrom.TryGetValue(current, out var parent))
            {
                current = parent;
                path.Insert(0, current);
            }
            return path;
        }

        private IEnumerable<string> GetNeighbors(string word)
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
                    if (_wordSet.Contains(cand))
                        yield return cand;
                }
                chars[i] = original;
            }
        }
    }
}
