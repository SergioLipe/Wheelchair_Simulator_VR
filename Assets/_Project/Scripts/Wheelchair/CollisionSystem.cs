using UnityEngine;
using System.Collections;

/// <summary>
/// Sistema de deteção e gestão de colisões para a cadeira de rodas
/// Responsável por: deteção de obstáculos, bloqueios direcionais, deslizamento em paredes
/// </summary>
public class CollisionSystem : MonoBehaviour
{
    [Header("=== Sistema de Colisão ===")]
    [Tooltip("Ativar sistema de deteção de colisões")]
    public bool avisosColisaoAtivos = true;

    [Tooltip("Distância para deteção de obstáculos")]
    public float distanciaAviso = 1.0f;

    [Tooltip("Distância REAL para bloqueio (metros) - quão perto pode chegar")]
    [Range(0.001f, 0.03f)]
    public float distanciaBloqueio = 0.008f;  

    [Tooltip("Mostrar raios de debug na Scene View")]
    public bool mostrarDebugRaios = true;

    [Header("=== Configuração Física ===")]
    [Tooltip("Confiar principalmente nas colisões físicas do CharacterController?")]
    public bool usarColisaoFisica = true;

    [Header("=== Estado de Colisão (Debug) ===")]
    [SerializeField] private bool emColisao = false;
    [SerializeField] private string objetoColidido = "";
    [SerializeField] private float distanciaObstaculo = 999f;
    [SerializeField] private bool bloqueadoFrente = false;
    [SerializeField] private bool bloqueadoTras = false;

    // Componentes externos (injetados)
    private CharacterController controller;
    private Transform transformCadeira;
    private CollisionFlashEffect flashEffect;

    // Variáveis de colisão
    private Vector3 normalColisao = Vector3.zero;
    private Vector3 pontoColisao = Vector3.zero;
    private float tempoColisao = 0f;
    private float ultimoTempoColisao = 0f;
    private bool avisoProximidade = false;

    // Sistema de bloqueio direcional
    private Vector3 direcaoBloqueada = Vector3.zero;
    private float tempoBloqueio = 0f;
    private const float duracaoBloqueio = 0.2f;

    // Sistema de deslizamento em paredes
    private bool deslizaParede = false;
    private Vector3 direcaoDeslize = Vector3.zero;

    /// <summary>
    /// Inicializar o sistema de colisão com as referências necessárias
    /// </summary>
    public void Inicializar(CharacterController characterController, Transform transform)
    {
        this.controller = characterController;
        this.transformCadeira = transform;

        // Obter ou criar o componente de flash
        flashEffect = GetComponent<CollisionFlashEffect>();
        if (flashEffect == null)
        {
            flashEffect = gameObject.AddComponent<CollisionFlashEffect>();
            Debug.Log("✅ CollisionFlashEffect criado automaticamente");
        }

        Debug.Log("✅ Sistema de Colisão inicializado!");
        Debug.Log($"📏 Distância de bloqueio: {distanciaBloqueio}m ({distanciaBloqueio * 100}cm)");
        Debug.Log($"🎯 Modo: {(usarColisaoFisica ? "Colisão Física REAL" : "Raycasts Preventivos")}");
    }

    /// <summary>
    /// Atualizar o sistema de colisão (chamar no Update)
    /// </summary>
    public void Atualizar()
    {
        // Se usar colisão física, APENAS mostrar raios visuais, NÃO bloquear
        if (usarColisaoFisica)
        {
            if (mostrarDebugRaios)
            {
                VerificarObstaculosVisual();
            }
        }
        else
        {
            // Modo antigo: usar raycasts para bloquear
            if (avisosColisaoAtivos)
            {
                VerificarObstaculosCompleto();
            }
        }

        // Atualizar temporizador de bloqueio
        if (tempoBloqueio > 0)
        {
            tempoBloqueio -= Time.deltaTime;
            if (tempoBloqueio <= 0)
            {
                direcaoBloqueada = Vector3.zero;
                bloqueadoFrente = false;
                bloqueadoTras = false;
            }
        }

        // Reset automático da colisão após 0.5 segundos
        if (emColisao && Time.time - tempoColisao > 0.5f)
        {
            emColisao = false;
            objetoColidido = "";
            deslizaParede = false;
        }
    }

    /// <summary>
    /// Verificar se pode mover para uma determinada posição
    /// </summary>
    public bool PodeMoverPara(Vector3 posicao)
    {
        // Se usar colisão física, SEMPRE permite - CharacterController decide
        if (usarColisaoFisica)
        {
            return true;
        }

        Vector3 origem = transformCadeira.position + Vector3.up * 0.5f;
        Vector3 direcao = (posicao - transformCadeira.position).normalized;
        float distancia = Vector3.Distance(transformCadeira.position, posicao);

        RaycastHit hit;
        if (Physics.Raycast(origem, direcao, out hit, distancia + 0.02f))
        {
            // Ignorar chão
            if (hit.collider.name.ToLower().Contains("plane") ||
                hit.collider.name.ToLower().Contains("ground") ||
                hit.collider.name.ToLower().Contains("floor"))
            {
                return true;
            }
            return false;
        }

        return true;
    }

    /// <summary>
    /// Verificar obstáculos apenas para VISUALIZAÇÃO (não bloqueia)
    /// </summary>
    void VerificarObstaculosVisual()
    {
        Vector3 centroController = transformCadeira.position + Vector3.up * 0.7f;
        
        avisoProximidade = false;
        float menorDist = 999f;

        // Raios frontais para visualização
        for (float offsetY = 0.3f; offsetY <= 0.9f; offsetY += 0.3f)
        {
            Vector3 origemLocal = centroController;
            origemLocal.y = transformCadeira.position.y + offsetY;
            
            Vector3 origemRaio = origemLocal + transformCadeira.forward * controller.radius;
            
            RaycastHit hit;

            if (Physics.Raycast(origemRaio, transformCadeira.forward, out hit, distanciaAviso))
            {
                string nomeObjeto = hit.collider.name.ToLower();
                if (nomeObjeto.Contains("plane") || nomeObjeto.Contains("ground") || nomeObjeto.Contains("floor"))
                    continue;

                float dist = hit.distance;

                if (dist < menorDist)
                {
                    menorDist = dist;
                    avisoProximidade = true;
                }

                Color corRaio = dist < 0.05f ? Color.red : 
                               (dist < 0.2f ? Color.yellow : Color.green);
                Debug.DrawRay(origemRaio, transformCadeira.forward * hit.distance, corRaio);
            }
            else
            {
                Debug.DrawRay(origemRaio, transformCadeira.forward * 0.5f, new Color(0.5f, 0.5f, 0.5f, 0.3f));
            }
        }

        distanciaObstaculo = menorDist;
        DesenharCilindroController();
    }

    /// <summary>
    /// Sistema completo de verificação de obstáculos em múltiplas direções
    /// </summary>
    void VerificarObstaculosCompleto()
    {
        Vector3 centroController = transformCadeira.position + Vector3.up * 0.7f;
        
        avisoProximidade = false;
        bloqueadoFrente = false;
        bloqueadoTras = false;
        float menorDist = 999f;
        string objetoMaisProximo = "";

        // === VERIFICAÇÃO FRONTAL ===
        for (float offsetX = -0.2f; offsetX <= 0.2f; offsetX += 0.1f)
        {
            for (float offsetY = 0.2f; offsetY <= 1.0f; offsetY += 0.4f)
            {
                Vector3 origemLocal = centroController + transformCadeira.right * offsetX;
                origemLocal.y = transformCadeira.position.y + offsetY;
                
                Vector3 origemRaio = origemLocal + transformCadeira.forward * controller.radius;
                
                RaycastHit hit;

                if (Physics.Raycast(origemRaio, transformCadeira.forward, out hit, distanciaAviso))
                {
                    string nomeObjeto = hit.collider.name.ToLower();
                    if (nomeObjeto.Contains("plane") || nomeObjeto.Contains("ground") || nomeObjeto.Contains("floor"))
                        continue;

                    float dist = hit.distance;

                    // BLOQUEIO quando realmente próximo
                    if (dist < distanciaBloqueio)
                    {
                        bloqueadoFrente = true;
                        tempoBloqueio = duracaoBloqueio;
                        normalColisao = hit.normal;

                        Vector3 projecao = Vector3.Project(transformCadeira.forward, hit.normal);
                        direcaoDeslize = (transformCadeira.forward - projecao).normalized;
                        deslizaParede = true;
                    }

                    if (dist < menorDist)
                    {
                        menorDist = dist;
                        objetoMaisProximo = hit.collider.name;
                        avisoProximidade = true;
                    }

                    if (mostrarDebugRaios)
                    {
                        Color corRaio = dist < distanciaBloqueio ? Color.red : 
                                       (dist < 0.3f ? Color.yellow : Color.green);
                        Debug.DrawRay(origemRaio, transformCadeira.forward * hit.distance, corRaio);
                    }
                }
                else if (mostrarDebugRaios)
                {
                    Debug.DrawRay(origemRaio, transformCadeira.forward * 0.5f, Color.gray);
                }

                // === VERIFICAÇÃO TRASEIRA ===
                Vector3 origemRaioTras = origemLocal - transformCadeira.forward * controller.radius;
                
                if (Physics.Raycast(origemRaioTras, -transformCadeira.forward, out hit, distanciaAviso * 0.5f))
                {
                    string nomeObjeto = hit.collider.name.ToLower();
                    if (nomeObjeto.Contains("plane") || nomeObjeto.Contains("ground") || nomeObjeto.Contains("floor"))
                        continue;

                    if (hit.distance < distanciaBloqueio)
                    {
                        bloqueadoTras = true;
                        tempoBloqueio = duracaoBloqueio;
                    }

                    if (mostrarDebugRaios)
                    {
                        Color corRaio = hit.distance < distanciaBloqueio ? Color.red : Color.magenta;
                        Debug.DrawRay(origemRaioTras, -transformCadeira.forward * hit.distance, corRaio);
                    }
                }
            }
        }

        distanciaObstaculo = menorDist;
        if (avisoProximidade && !emColisao)
        {
            objetoColidido = objetoMaisProximo;
        }

        if (mostrarDebugRaios)
        {
            DesenharCilindroController();
        }
    }

    /// <summary>
    /// Desenhar o cilindro do CharacterController na Scene View (debug visual)
    /// </summary>
    void DesenharCilindroController()
    {
        Vector3 centro = transformCadeira.position + controller.center;
        float radius = controller.radius;
        float altura = controller.height;

        Color cor = bloqueadoFrente || bloqueadoTras ? Color.red : Color.green;
        cor.a = 0.3f;

        // Desenhar círculo superior
        for (int i = 0; i < 16; i++)
        {
            float angulo1 = i * 22.5f * Mathf.Deg2Rad;
            float angulo2 = (i + 1) * 22.5f * Mathf.Deg2Rad;

            Vector3 p1 = centro + new Vector3(Mathf.Cos(angulo1) * radius, altura/2, Mathf.Sin(angulo1) * radius);
            Vector3 p2 = centro + new Vector3(Mathf.Cos(angulo2) * radius, altura/2, Mathf.Sin(angulo2) * radius);

            Debug.DrawLine(p1, p2, cor);

            // Círculo inferior
            Vector3 p3 = centro + new Vector3(Mathf.Cos(angulo1) * radius, -altura/2, Mathf.Sin(angulo1) * radius);
            Vector3 p4 = centro + new Vector3(Mathf.Cos(angulo2) * radius, -altura/2, Mathf.Sin(angulo2) * radius);

            Debug.DrawLine(p3, p4, cor);

            // Linhas verticais
            if (i % 4 == 0)
            {
                Debug.DrawLine(p1, p3, cor);
            }
        }
    }

    /// <summary>
    /// Processar colisões do CharacterController
    /// </summary>
    public void ProcessarColisao(ControllerColliderHit hit, float velocidadeAtual, ref float velocidadeAtualRef)
    {
        // Ignorar chão
        string nome = hit.gameObject.name.ToLower();
        if (nome.Contains("plane") || nome.Contains("ground") || nome.Contains("floor"))
            return;

        // Evitar múltiplas deteções
        if (Time.time - ultimoTempoColisao < 0.1f) return;

        // Determinar direção da colisão
        Vector3 dirParaObstaculo = (hit.point - transformCadeira.position);
        dirParaObstaculo.y = 0;
        dirParaObstaculo.Normalize();

        float angulo = Vector3.Angle(transformCadeira.forward, dirParaObstaculo);

        // BLOQUEIO baseado no ângulo + ATIVAR FLASH
        if (angulo < 60f)  // Colisão frontal
        {
            bloqueadoFrente = true;
            velocidadeAtualRef = 0;
            
            // Ativar flash frontal
            if (flashEffect != null)
                flashEffect.FlashFrontal();
            
            Debug.Log($"💥 COLISÃO FRONTAL com {hit.gameObject.name}");
        }
        else if (angulo > 120f)  // Colisão traseira
        {
            bloqueadoTras = true;
            velocidadeAtualRef = 0;
            
            // Ativar flash traseiro
            if (flashEffect != null)
                flashEffect.FlashTraseiro();
            
            Debug.Log($"💥 COLISÃO TRASEIRA com {hit.gameObject.name}");
        }
        else  // Colisão lateral
        {
            normalColisao = hit.normal;
            Vector3 projecao = Vector3.Project(transformCadeira.forward, normalColisao);
            direcaoDeslize = (transformCadeira.forward - projecao).normalized;
            deslizaParede = true;

            // Determinar se é esquerda ou direita
            float lado = Vector3.Dot(transformCadeira.right, dirParaObstaculo);
            
            // Ativar flash lateral
            if (flashEffect != null)
            {
                if (lado > 0)
                    flashEffect.FlashLateralDireito();
                else
                    flashEffect.FlashLateralEsquerdo();
            }

            Debug.Log($"💥 COLISÃO LATERAL ({(lado > 0 ? "Direita" : "Esquerda")}) com {hit.gameObject.name}");
        }

        emColisao = true;
        objetoColidido = hit.gameObject.name;
        pontoColisao = hit.point;
        tempoColisao = Time.time;
        ultimoTempoColisao = Time.time;
        tempoBloqueio = duracaoBloqueio;
    }

    /// <summary>
    /// Limpar estado de deslizamento em parede
    /// </summary>
    public void LimparDeslizamento()
    {
        deslizaParede = false;
        direcaoDeslize = Vector3.zero;
    }

    // ===== PROPRIEDADES PÚBLICAS (Getters) =====

    public bool EstaBloqueadoFrente 
    {
        get
        {
            if (usarColisaoFisica)
            {
                return bloqueadoFrente && (Time.time - ultimoTempoColisao < duracaoBloqueio);
            }
            return bloqueadoFrente;
        }
    }

    public bool EstaBloqueadoTras 
    {
        get
        {
            if (usarColisaoFisica)
            {
                return bloqueadoTras && (Time.time - ultimoTempoColisao < duracaoBloqueio);
            }
            return bloqueadoTras;
        }
    }

    public bool EstaDeslizandoParede => deslizaParede;
    public Vector3 DirecaoDeslize => direcaoDeslize;
    public bool EstaEmColisao => emColisao;
    public string ObjetoColidido => objetoColidido;
    public float DistanciaObstaculo => distanciaObstaculo;
}