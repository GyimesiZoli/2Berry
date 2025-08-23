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

        public WordChainController(WordList wordService)
        {
            _wordService = wordService;
        }

        [HttpPost]
        public IActionResult GetWordChain([FromBody] WordChainRequest request)
        {
            var startWord = request.Source.Trim().ToLower();
            if (string.IsNullOrEmpty(startWord))
            {
                return BadRequest($"A kezdőszó üres, kérek helyette egy 5 betűs szót!");
            }
            else if (startWord.Length != 5)
            {
                return BadRequest($"A kezdőszó {startWord.Length} betűből áll, kérek helyette egy 5 betűst!");
            }
            var endWord = request.Target.Trim().ToLower();
            if (string.IsNullOrEmpty(endWord))
            {
                return BadRequest($"A végszó üres, kérek helyette egy 5 betűs szót!");
            }
            else if (endWord.Length != 5)
            {
                return BadRequest($"A végszó {endWord.Length} betűből áll, kérek helyette egy 5 betűst!");
            }
 
            var words = _wordService.GetWords();
            var wordSet = new HashSet<string>(words.Where(w => w.Length == 5));

            var path = FindWordChainAStar(startWord, endWord, wordSet);

            if (path == null || path.Count == 0)
            {
                return NotFound("Sajnos tudok láncot alkotni a két szó között :(");
            }

            return Ok(path);
        }

        private List<string>? FindWordChainAStar(string start, string end, HashSet<string> wordSet)
        {
            if (!wordSet.Contains(start) || !wordSet.Contains(end))
                return null;

            var cameFrom = new Dictionary<string, string?>(); 
            var gScore = new Dictionary<string, int>();      

            foreach (var w in wordSet)
                gScore[w] = int.MaxValue;

            gScore[start] = 0;

            var open = new PriorityQueue<string, int>();
            open.Enqueue(start, Heuristic(start, end));

            var closed = new HashSet<string>();

            while (open.Count > 0)
            {
                var current = open.Dequeue();

                if (current == end)
                    return ReconstructPath(cameFrom, current);

                closed.Add(current);

                foreach (var neighbor in GetNeighbors(current, wordSet))
                {
                    if (closed.Contains(neighbor))
                        continue;

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
            {
                if (word[i] != target[i])
                    diff++;
            }
            return diff;
        }

        private List<string> ReconstructPath(Dictionary<string, string?> cameFrom, string current)
        {
            var path = new List<string> { current };
            while (cameFrom.ContainsKey(current))
            {
                current = cameFrom[current]!;
                if (current != null)
                    path.Insert(0, current);
            }
            return path;
        }

        private IEnumerable<string> GetNeighbors(string word, HashSet<string> wordSet)
        {
            var chars = word.ToCharArray();

            for (int i = 0; i < chars.Length; i++)
            {
                char original = chars[i];

                for (char c = 'a'; c <= 'z'; c++)
                {
                    if (c == original) continue;

                    chars[i] = c;
                    var newWord = new string(chars);

                    if (wordSet.Contains(newWord))
                        yield return newWord;
                }

                chars[i] = original;
            }
        }
    }
}
