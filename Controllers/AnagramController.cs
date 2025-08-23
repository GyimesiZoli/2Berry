namespace WordGame.Controllers
{
    using Microsoft.AspNetCore.Mvc;
    using WordGame.Tools;

    [ApiController]
    [Route("[controller]")]
    public class AnagramController : ControllerBase
    {
        private readonly WordList _wordService;

        public AnagramController(WordList wordService)
        {
            _wordService = wordService;
        }

        [HttpPost]
        public IActionResult GetAnagrams([FromBody] Models.AnagramRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.Word) || request.Word.Length != 5)
            {
                return BadRequest($"Hiba: A bevitt szó {request.Word.Length} betűből áll 5 helyett.");
            }


            var words = _wordService.GetWords();
            var inputWorld = request.Word.ToLower();
            var anagrams = words
                .Where(w => w.Length == 5 &&
                            w.OrderBy(c => c).SequenceEqual(inputWorld.OrderBy(c => c)))
                .ToList();
            if (anagrams.Contains(inputWorld))
            {
                anagrams.Remove(inputWorld);
            }
            return Ok(anagrams);
        }
    }
}
