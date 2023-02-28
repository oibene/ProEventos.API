using System.IO;
using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using ProEventos.Application.Contratos;
using ProEventos.Application.Dtos;
using Microsoft.AspNetCore.Hosting;
using System.Linq;

namespace ProEventos.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]

    public class EventosController : ControllerBase
    {
        private readonly IEventoService _eventoService;
        private readonly IWebHostEnvironment _hostEnv;
        
        public EventosController(IEventoService eventoService, IWebHostEnvironment hostEnv)
        {
            _eventoService = eventoService;
            _hostEnv = hostEnv;
        }

        [HttpGet]
        public async Task<IActionResult> Get()
        {
            //IActionResult retorna status (404, 303 etc)
            try
            {
                var eventos = await _eventoService.GetAllEventosAsync(true);
                if (eventos == null) return NoContent();

                return Ok(eventos);
                
            }
            catch (Exception ex)
            {
                return this.StatusCode(StatusCodes.Status500InternalServerError,
                $"Erro ao tentar recuperar eventos. Erro: {ex.Message}");
                
            }
        }

        [HttpGet ("{id}")]
        public async Task<IActionResult> GetById(int id)
        {
            try
            {
                var evento = await _eventoService.GetEventobyIdAsync(id, true);
                if (evento == null) return NoContent();

                return Ok(evento);
                
            }
            catch (Exception ex)
            {
                return this.StatusCode(StatusCodes.Status500InternalServerError,
                $"Erro ao tentar recuperar eventos. Erro: {ex.Message}");
                
            }
        }

        [HttpGet ("{tema}/tema")] // diferencia o id do tema pro http
        public async Task<IActionResult> GetByTema(string tema)
        {
            try
            {
                var evento = await _eventoService.GetAllEventosbyTemasAsync(tema, true);
                if (evento == null) return NoContent();

                return Ok(evento);
                
            }
            catch (Exception ex)
            {
                return this.StatusCode(StatusCodes.Status500InternalServerError,
                $"Erro ao tentar recuperar eventos. Erro: {ex.Message}");
                
            }
        }

        [HttpPost]
        public async Task<IActionResult> Post(EventoDto model)
        {
            try
            {
                var evento = await _eventoService.AddEvento(model);
                if (evento == null) return NoContent();

                return Ok(evento);
                
            }
            catch (Exception ex)
            {
                return this.StatusCode(StatusCodes.Status500InternalServerError,
                $"Erro ao tentar adicionar eventos. Erro: {ex.Message}");
                
            }
        }


        // UPLOAD DE IMAGENS
        [HttpPost("upload-imagem/{eventoId}")]
        public async Task<IActionResult> UploadImage(int eventoId)
        {
            try
            {
                //se meu evento existe 
                var evento = await _eventoService.GetEventobyIdAsync(eventoId, true);
                if (evento == null) return NoContent();

                //ve quem chamou meu metodo Post e abre o primeiro arquivo
                var file = Request.Form.Files[0];
                if (file.Length > 0)
                {
                    DeleteImage(evento.ImagemURL);
                    evento.ImagemURL = await SaveImage(file);
                }

                //ele atualiza o nome da imagem no banco de dados
                var eventoRetorno = await _eventoService.UpdateEvento(eventoId, evento);
                return Ok(eventoRetorno);
            }
            catch (Exception ex)
            {
                return this.StatusCode(StatusCodes.Status500InternalServerError,
                $"Erro ao tentar adicionar eventos. Erro: {ex.Message}");
                
            }
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> Put(int id, EventoDto model)
        {
            try
            {
                var evento = await _eventoService.UpdateEvento(id, model);
                if (evento == null) return NoContent();

                return Ok(evento);
                
            }
            catch (Exception ex)
            {
                return this.StatusCode(StatusCodes.Status500InternalServerError,
                $"Erro ao tentar atualizar eventos. Erro: {ex.Message}");
                
            }
        }


        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            try
            {
                var evento = await _eventoService.GetEventobyIdAsync(id, true);
                if (evento == null) return NoContent();

                if (await _eventoService.DeleteEvento(id))
                {
                    DeleteImage(evento.ImagemURL);
                    return Ok(new { message = "Deletado"}); //envia mensagem
                
                }
                else
                    throw new Exception("Ocorreu um erro não especificado ao tentar deletar Evento");

            }
            catch (Exception ex)
            {
                return this.StatusCode(StatusCodes.Status500InternalServerError,
                $"Erro ao tentar deletar eventos. Erro: {ex.Message}");
                
            }
        }


        [NonAction] //nao é um endpoint (nao pode ser acessado por fora)
        public async Task<string> SaveImage(IFormFile imgFile){

            //pega imagem sem a extensão e corta ela
            string imgName = new String(Path.GetFileNameWithoutExtension
                                        (imgFile.FileName).Take(10).ToArray()
                                        ).Replace(' ', '-');

            //faz a img ser extremamente unica add data e a extensao de volta
            imgName = $"{imgName}{DateTime.UtcNow.ToString("yymmssfff")}{Path.GetExtension(imgFile.FileName)}";

            var imgPath = Path.Combine(_hostEnv.ContentRootPath, @"resources/images", imgName);

            using (var fileStream = new FileStream(imgPath, FileMode.Create))
            {
                await imgFile.CopyToAsync(fileStream);
            }

            return imgName;
        }

        [NonAction]
        public void DeleteImage(string imgName){
            //pega a raiz atual e concatena com o diretorio e o nome da nossa imagem
            var imgPath = Path.Combine(_hostEnv.ContentRootPath, @"resources/images", imgName);
            //se ele existe o arquivo é apagado
            if (System.IO.File.Exists(imgPath))
                System.IO.File.Delete(imgPath);
        }

    }
}