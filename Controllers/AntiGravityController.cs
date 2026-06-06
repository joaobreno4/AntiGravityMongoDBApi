using Microsoft.AspNetCore.Mvc;
using MongoDB.Driver;
using MongoDB.Bson;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace AntiGravityMongoDBApi.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AntiGravityController : ControllerBase
    {
        private readonly IMongoDatabase _database;
        private readonly IMongoCollection<ElementoFlutuante> _collection;

        // ============================================================================
        // 🧲 INJEÇÃO DE DEPENDÊNCIA RÍGIDA (CABOS DE AÇO)
        // ============================================================================
        // O construtor do Controller está ancorado ao motor de Injeção de Dependências.
        // Isso impede que as referências do banco de dados entrem em rota de colisão
        // na memória ou fiquem à deriva no Heap.
        public AntiGravityController(IMongoDatabase database)
        {
            _database = database ?? throw new ArgumentNullException(nameof(database), "O cabo de ancoragem do MongoDB está nulo!");
            _collection = _database.GetCollection<ElementoFlutuante>("ElementosDOM");
        }

        // ============================================================================
        // 🛰️ CAPTURA DE CORPOS CAÍDOS E COLIDIDOS (POST: api/antigravity/colisao)
        // ============================================================================
        [HttpPost("colisao")]
        public async Task<IActionResult> RegistrarColisao([FromBody] ElementoFlutuante elemento)
        {
            if (elemento == null)
            {
                return BadRequest(new { erro = "Dados nulos flutuando no vácuo de rede. Envie um corpo rígido válido!" });
            }

            // Ancorando o ID antes de salvar na órbita do Atlas
            if (string.IsNullOrEmpty(elemento.Id))
            {
                elemento.Id = Guid.NewGuid().ToString();
            }

            try
            {
                // Inserção assíncrona: empurra o documento para o cluster em órbita
                await _collection.InsertOneAsync(elemento);
                return Ok(new 
                { 
                    mensagem = $"Elemento '{elemento.NomeId}' ancorado com sucesso no MongoDB Atlas!", 
                    id = elemento.Id,
                    massaCalculada = elemento.Largura * elemento.Altura * 0.01 // Cálculo baseado nas dimensões reais do DOM
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new 
                { 
                    erro = "O cabo de ancoragem de rede se rompeu durante a escrita no Cluster em órbita.",
                    detalhe = ex.Message 
                });
            }
        }

        // ============================================================================
        // 📡 RECUPERAR CORPOS EM ÓRBITA (GET: api/antigravity/orbita)
        // ============================================================================
        [HttpGet("orbita")]
        public async Task<IActionResult> ObterElementosEmOrbita()
        {
            try
            {
                // Recupera todos os documentos BSON sem filtros restritivos (tudo que está flutuando)
                var elementos = await _collection.Find(new BsonDocument()).ToListAsync();
                return Ok(elementos);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new 
                { 
                    erro = "Falha de rádio ao tentar se comunicar com o Cluster remoto.", 
                    detalhe = ex.Message 
                });
            }
        }
    }

    // ============================================================================
    // 📦 CORPO RÍGIDO DO DOM (MODELO DE MAPEAMENTO DE MASSA E FÍSICA)
    // ============================================================================
    // Representa os elementos do frontend que caíram na tela e cujas coordenadas 
    // de física 2D (Box2D) precisam ser preservadas.
    public class ElementoFlutuante
    {
        public string? Id { get; set; }
        
        // Identificador no DOM (ex: 'logo-google', 'btn-pesquisa')
        public string NomeId { get; set; } = "elemento-anonimo";
        
        // Coordenadas calculadas pelo motor físico (Box2D) na tela do navegador
        public double CoordenadaX { get; set; }
        public double CoordenadaY { get; set; }
        
        // Vetores de velocidade física no instante da captura
        public double VelocidadeX { get; set; }
        public double VelocidadeY { get; set; }
        public double AnguloRotacao { get; set; }
        
        // Dimensões usadas para mapear a massa física do corpo rígido
        public double Largura { get; set; }
        public double Altura { get; set; }
        
        // Flexibilidade de Schemas: se houver destroços acoplados ao corpo físico,
        // eles grudam neste dicionário dinâmico sem quebrar o mapeamento.
        public Dictionary<string, object>? DestrocosAcoplados { get; set; }
    }
}
