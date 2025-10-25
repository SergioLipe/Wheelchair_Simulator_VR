using UnityEngine;
using System.Collections;
using System.Collections.Generic;

/// <summary>
/// Controlador principal de movimento da cadeira de rodas elétrica
/// Responsável por: input, velocidade, aceleração, rotação e física
/// </summary>
public class Movement : MonoBehaviour
{
    [Header("=== Configurações de Velocidade ===")]
    [Tooltip("Velocidade máxima em modo normal (km/h)")]
    public float velocidadeMaximaNormal = 6f;

    [Tooltip("Velocidade máxima em modo lento/interior (km/h)")]
    public float velocidadeMaximaLenta = 3f;

    [Tooltip("Velocidade de marcha-atrás (km/h)")]
    public float velocidadeMarchaAtras = 2f;

    [Header("=== Configurações de Aceleração ===")]
    [Tooltip("Tempo para atingir velocidade máxima (segundos)")]
    public float tempoAceleracao = 2f;

    [Tooltip("Tempo para parar completamente (segundos)")]
    public float tempoTravagem = 1.5f;

    [Header("=== Configurações de Rotação ===")]
    [Tooltip("Velocidade de rotação (graus por segundo)")]
    public float velocidadeRotacao = 45f;

    [Tooltip("Pode rodar sem se mover para frente/trás? (Só funciona com direção frontal)")]
    public bool rotacaoNoLugar = false;

    [Header("=== Modos de Condução ===")]
    [Tooltip("Modo atual de velocidade")]
    public ModosVelocidade modoAtual = ModosVelocidade.Normal;

    // --- NOVA SECÇÃO DE SONS ---
    [Header("=== Sons de Efeitos (One-Shot) ===")]
    [Tooltip("O 'lançador' de áudio para efeitos curtos (cliques, colisões)")]
    public AudioSource audioEfeitos;

    [Tooltip("Som a tocar quando muda o modo de velocidade (teclas 1, 2)")]
    public AudioClip somMudarModo;

    [Tooltip("Som a tocar quando muda o tipo de direção (tecla T)")]
    public AudioClip somMudarDirecao;

    [Tooltip("Som a tocar quando bate com força")]
    public AudioClip somColisaoForte;

    [Tooltip("Som a tocar quando começa a deslizar numa parede")]
    public AudioClip somDeslizarInicio;

    [Tooltip("Velocidade mínima (em m/s) para o som de colisão tocar (opcional)")]
    public float velMinimaColisao = 0.8f;
    // --- FIM DA NOVA SECÇÃO ---

    [Header("=== Física e Limites ===")]
    [Tooltip("Inclinação máxima que consegue subir (graus)")]
    public float inclinacaoMaxima = 10f;

    [Tooltip("Gravidade aplicada")]
    public float gravidade = -9.81f;

    [Header("=== Estado Atual (Debug) ===")]
    [SerializeField] private float velocidadeAtual = 0f;
    [SerializeField] private float velocidadeDesejada = 0f;
    [SerializeField] private bool travaoDeEmergencia = false;
    [SerializeField] private string tipoDirecaoAtual = "Frontal";
    [SerializeField] private float eficienciaRotacao = 100f;

    // Componentes
    private CharacterController controller;
    private Vector3 movimentoVelocidade;
    private WheelController wheelController;

    // Sistema de colisão (separado)
    private CollisionSystem sistemaColisao;

    // Sistema de input suavizado
    private float inputVerticalSuavizado = 0f;
    private float inputHorizontalSuavizado = 0f;

    // Direção traseira - feedback
    private bool tentandoVirarParado = false;
    private float tempoTentandoVirar = 0f;

    // Variável pública para o script de som saber se o jogador está a acelerar
    [HideInInspector] // Esconde do Inspetor, mas é pública
    public bool jogadorEstaAcelerando = false;

    // Cache para sons (para não repetir)
    private bool estaDeslizandoCache = false;
    private string tipoDirecaoCache = "Frontal";
    private bool estaEmColisaoCache = false;


    public enum ModosVelocidade
    {
        Lento,
        Normal,
        Desligado
    }

    void Start()
    {
        // Configurar o CharacterController
        controller = GetComponent<CharacterController>();
        if (controller == null)
        {
            controller = gameObject.AddComponent<CharacterController>();
        }

        // === VALORES ABSOLUTOS MÍNIMOS ===
        controller.height = 1.4f;
        controller.radius = 0.2f;  // 20cm - MÍNIMO para não atravessar paredes
        controller.center = new Vector3(0, 0.7f, 0);

        // === SkinWidth ZERO (ou quase) ===
        controller.skinWidth = 0.0001f;  // 0.1mm - PRATICAMENTE ZERO!
        controller.minMoveDistance = 0.0f;  // ZERO absoluto
        controller.stepOffset = 0.1f;

        // Elevar um pouco no início
        transform.position += Vector3.up * 0.1f;

        // Obter referência ao wheel controller
        wheelController = GetComponent<WheelController>();

        // === INICIALIZAR SISTEMA DE COLISÃO ===
        sistemaColisao = GetComponent<CollisionSystem>();
        if (sistemaColisao == null)
        {
            sistemaColisao = gameObject.AddComponent<CollisionSystem>();
        }
        sistemaColisao.Inicializar(controller, transform);

        // Converter km/h para m/s
        velocidadeMaximaNormal = velocidadeMaximaNormal / 3.6f;
        velocidadeMaximaLenta = velocidadeMaximaLenta / 3.6f;
        velocidadeMarchaAtras = velocidadeMarchaAtras / 3.6f;

        // Cache inicial da direção
        if (wheelController != null)
        {
            tipoDirecaoCache = wheelController.GetTipoDirecao().ToString();
        }

        Debug.Log("✅ Cadeira de Rodas (Movement.cs) inicializada!");
    }
    void Update()
    {
        // Atualizar tipo de direção para debug
        if (wheelController != null)
        {
            tipoDirecaoAtual = wheelController.GetTipoDirecao().ToString();

            // --- LÓGICA DE SOM (MUDAR DIREÇÃO) ---
            if (tipoDirecaoAtual != tipoDirecaoCache)
            {
                TocarSom(somMudarDirecao);
                tipoDirecaoCache = tipoDirecaoAtual; // Atualiza o cache
            }
            // --- FIM DA LÓGICA ---
        }

        // === ATUALIZAR SISTEMA DE COLISÃO ===
        sistemaColisao.Atualizar();


        // --- LÓGICA DE SOM (DESLIZAR) ---
        bool aDeslizarAgora = sistemaColisao.EstaDeslizandoParede;
        if (aDeslizarAgora && !estaDeslizandoCache)
        {
            TocarSom(somDeslizarInicio); // Toca só no início do deslize
        }
        estaDeslizandoCache = aDeslizarAgora; // Atualiza o cache


        // --- LÓGICA DE SOM (COLISÃO) ---
        // 1. Definir o que é "Estar em Colisão"
        bool emColisaoAgora = (sistemaColisao.EstaEmColisao || sistemaColisao.EstaBloqueadoFrente || sistemaColisao.EstaBloqueadoTras);
        
        // 2. O deslize tem prioridade (para não tocar os dois sons ao mesmo tempo)
        if (aDeslizarAgora)
        {
            emColisaoAgora = false;
        }

        // 3. Tocar o som APENAS NO FRAME em que o estado muda
        if (emColisaoAgora && !estaEmColisaoCache)
        {
            // Opcional: Filtro de velocidade (se não quiseres som ao "encostar")
            // if (Mathf.Abs(velocidadeAtual) > velMinimaColisao)
            // {
                 TocarSom(somColisaoForte);
            // }
        }
        
        // 4. Atualizar o cache de colisão para o próximo frame
        estaEmColisaoCache = emColisaoAgora;
        // --- FIM DA LÓGICA ---


        // Atualizar temporizador do aviso de direção traseira
        if (tempoTentandoVirar > 0)
        {
            tempoTentandoVirar -= Time.deltaTime;
        }

        // Mudar modos com teclas numéricas
        GerirModos();

        // Processar movimento apenas se não estiver em modo desligado
        if (modoAtual != ModosVelocidade.Desligado)
        {
            ProcessarInputRealista();
            AplicarMovimentoRealista();
        }
        else
        {
            PararDeEmergencia();
            // CRÍTICO: Mesmo com travão ativo, aplicar gravidade ao CharacterController
            AplicarMovimentoVertical();
        }

        // Aplicar sempre a gravidade
        AplicarGravidade();
    }

    void GerirModos()
    {
        // Tecla 1: Modo Lento
        if (Input.GetKeyDown(KeyCode.Alpha1))
        {
            modoAtual = ModosVelocidade.Lento;
            Debug.Log("Modo: LENTO (Interior) - 3 km/h");
            TocarSom(somMudarModo); // --- TOCAR SOM ---
        }
        // Tecla 2: Modo Normal
        else if (Input.GetKeyDown(KeyCode.Alpha2))
        {
            modoAtual = ModosVelocidade.Normal;
            Debug.Log("Modo: NORMAL - 6 km/h");
            TocarSom(somMudarModo); // --- TOCAR SOM ---
        }
        // Espaço: Travão de emergência
        else if (Input.GetKeyDown(KeyCode.Space))
        {
            modoAtual = ModosVelocidade.Desligado;
            travaoDeEmergencia = true;
            Debug.Log("TRAVÃO DE EMERGÊNCIA ATIVADO!");
        }
        // Soltar espaço: Voltar ao modo normal
        else if (Input.GetKeyUp(KeyCode.Space))
        {
            modoAtual = ModosVelocidade.Normal;
            travaoDeEmergencia = false;
        }
    }

    void ProcessarInputRealista()
    {
        // Obter input do jogador
        float inputVertical = Input.GetAxis("Vertical");
        float inputHorizontal = Input.GetAxis("Horizontal");

        // Verifica se o input (antes de ser suavizado) é significativo
        jogadorEstaAcelerando = (Mathf.Abs(inputVertical) > 0.1f);

        // Suavizar o input
        float suavizacao = 3f;
        inputVerticalSuavizado = Mathf.Lerp(inputVerticalSuavizado, inputVertical, suavizacao * Time.deltaTime);
        inputHorizontalSuavizado = Mathf.Lerp(inputHorizontalSuavizado, inputHorizontal, suavizacao * Time.deltaTime);

        // Determinar velocidade máxima baseada no modo
        float velocidadeMaxima = modoAtual == ModosVelocidade.Lento ?
                                 velocidadeMaximaLenta : velocidadeMaximaNormal;

        // === SISTEMA DE BLOQUEIO REALISTA ===

        // Se está bloqueado à frente, NÃO permite movimento frontal
        if (sistemaColisao.EstaBloqueadoFrente && inputVerticalSuavizado > 0)
        {
            inputVerticalSuavizado = 0;
            velocidadeDesejada = 0;

            if (inputVertical > 0.5f)
            {
                velocidadeAtual = Mathf.Max(velocidadeAtual - 0.5f * Time.deltaTime, -0.05f);
            }
        }
        // Se está bloqueado atrás, NÃO permite marcha-atrás
        else if (sistemaColisao.EstaBloqueadoTras && inputVerticalSuavizado < 0)
        {
            inputVerticalSuavizado = 0;
            velocidadeDesejada = 0;
        }
        // Movimento normal quando não bloqueado
        else
        {
            if (inputVerticalSuavizado < 0)
            {
                velocidadeMaxima = velocidadeMarchaAtras;
            }

            velocidadeDesejada = inputVerticalSuavizado * velocidadeMaxima;
        }

        // === ACELERAÇÃO E DESACELERAÇÃO ===

        if (!sistemaColisao.EstaBloqueadoFrente && !sistemaColisao.EstaBloqueadoTras &&
            Mathf.Abs(velocidadeDesejada) > Mathf.Abs(velocidadeAtual))
        {
            float aceleracao = velocidadeMaxima / tempoAceleracao;
            velocidadeAtual = Mathf.MoveTowards(velocidadeAtual, velocidadeDesejada, aceleracao * Time.deltaTime);
        }
        else
        {
            float desaceleracao = velocidadeMaxima / tempoTravagem;

            if (sistemaColisao.EstaBloqueadoFrente || sistemaColisao.EstaBloqueadoTras)
            {
                velocidadeAtual = 0;
            }
            else if (sistemaColisao.EstaEmColisao)
            {
                desaceleracao *= 2f;
                velocidadeAtual = Mathf.MoveTowards(velocidadeAtual, velocidadeDesejada, desaceleracao * Time.deltaTime);
            }
            else
            {
                velocidadeAtual = Mathf.MoveTowards(velocidadeAtual, velocidadeDesejada, desaceleracao * Time.deltaTime);
            }
        }

        // Rotação
        ProcessarRotacao(inputHorizontalSuavizado);
    }

    void ProcessarRotacao(float inputHorizontal)
    {
        float multiplicadorRotacao = 1f;
        bool isDirecaoTraseira = false;
        eficienciaRotacao = 100f;

        if (wheelController != null)
        {
            isDirecaoTraseira = wheelController.GetTipoDirecao() == WheelController.TipoDirecao.DirecaoTraseira;

            if (isDirecaoTraseira)
            {
                multiplicadorRotacao = 1.3f;
            }
        }

        bool estaParado = Mathf.Abs(velocidadeAtual) < 0.1f;

        if (isDirecaoTraseira)
        {
            if (estaParado)
            {
                eficienciaRotacao = 0f;

                if (Mathf.Abs(inputHorizontal) > 0.1f)
                {
                    tentandoVirarParado = true;
                    tempoTentandoVirar = 1f;

                }

                return;
            }
            else
            {
                tentandoVirarParado = false;
                float velocidadeNormalizada = Mathf.Abs(velocidadeAtual) / velocidadeMaximaNormal;
                float eficienciaBase = Mathf.Lerp(0.2f, 1f, velocidadeNormalizada);
                multiplicadorRotacao *= eficienciaBase;

                if (velocidadeAtual < 0)
                {
                    multiplicadorRotacao *= -0.8f;
                    eficienciaRotacao = eficienciaBase * 80f;
                }
                else
                {
                    eficienciaRotacao = eficienciaBase * 100f;
                }
            }
        }
        else
        {
            tentandoVirarParado = false;

            if (estaParado && !rotacaoNoLugar)
            {
                eficienciaRotacao = 0f;
                return;
            }
            else if (estaParado && rotacaoNoLugar)
            {
                multiplicadorRotacao *= 1.5f;
                eficienciaRotacao = 100f;
            }
            else
            {
                float velocidadeNormalizada = Mathf.Abs(velocidadeAtual) / velocidadeMaximaNormal;
                multiplicadorRotacao *= (1f + velocidadeNormalizada * 0.2f);
                eficienciaRotacao = 100f;
            }
        }

        float rotacao = inputHorizontal * velocidadeRotacao * multiplicadorRotacao * Time.deltaTime;
        transform.Rotate(0, rotacao, 0);
    }

    void AplicarMovimentoRealista()
    {
        Vector3 direcaoMovimento = Vector3.zero;

        // === USAR SISTEMA DE COLISÃO PARA DESLIZAMENTO ===
        if (sistemaColisao.EstaDeslizandoParede && sistemaColisao.DirecaoDeslize != Vector3.zero)
        {
            direcaoMovimento = sistemaColisao.DirecaoDeslize * Mathf.Abs(velocidadeAtual) * 0.5f;
        }
        else
        {
            direcaoMovimento = transform.forward * velocidadeAtual;
        }

        direcaoMovimento.y = movimentoVelocidade.y;

        controller.Move(direcaoMovimento * Time.deltaTime);
    }

    void AplicarMovimentoVertical()
    {
        Vector3 movimentoVertical = new Vector3(0, movimentoVelocidade.y, 0);
        controller.Move(movimentoVertical * Time.deltaTime);
    }

    void AplicarGravidade()
    {
        if (controller.isGrounded)
        {
            movimentoVelocidade.y = -0.5f;
        }
        else
        {
            // Gravidade normal quando no ar
            movimentoVelocidade.y += gravidade * Time.deltaTime;

            //  Limitar velocidade máxima de queda (evita bugs)
            movimentoVelocidade.y = Mathf.Max(movimentoVelocidade.y, -20f);
        }
    }

    void PararDeEmergencia()
    {
        velocidadeAtual = 0;
        velocidadeDesejada = 0;

        // Limpar deslizamento através do sistema de colisão
        sistemaColisao.LimparDeslizamento();

        if (wheelController != null)
        {
            wheelController.PararRodas();
        }
    }

    /// <summary>
    /// Callback do Unity quando o CharacterController colide
    /// Delega a lógica ao sistema de colisão
    /// </summary>
    void OnControllerColliderHit(ControllerColliderHit hit)
    {
        // A lógica de som foi movida para o Update() para
        // detetar apenas a *mudança* de estado.
        sistemaColisao.ProcessarColisao(hit, velocidadeAtual, ref velocidadeAtual);
    }

    // ===== MÉTODOS PÚBLICOS =====

    public float GetVelocidadeNormalizada()
    {
        return velocidadeAtual / velocidadeMaximaNormal;
    }

    public bool EstaEmMovimento()
    {
        return Mathf.Abs(velocidadeAtual) > 0.1f;
    }

    public void ReduzirVelocidade(float multiplicador)
    {
        velocidadeAtual *= multiplicador;
    }

    // --- NOVA FUNÇÃO PÚBLICA PARA TOCAR SONS ---
    /// <summary>
    /// Toca um AudioClip uma vez no 'lançador' de efeitos
    /// </summary>
    public void TocarSom(AudioClip clip)
    {
        // Verifica se o lançador e o clip existem antes de tocar
        if (audioEfeitos != null && clip != null)
        {
            audioEfeitos.PlayOneShot(clip);
        }
    }
    // --- FIM DA NOVA FUNÇÃO ---


    // ===== GUI DE DEBUG ORIGINAL =====

    void OnGUI()
    {
        if (!Application.isEditor) return;

        // Info de movimento
        GUI.color = new Color(0, 0, 0, 0.8f);
        GUI.Box(new Rect(10, 100, 250, 110), "");

        GUI.color = Color.white;
        GUI.Label(new Rect(15, 105, 240, 20), "=== CADEIRA DE RODAS ===");
        GUI.Label(new Rect(15, 125, 240, 20), $"Modo: {modoAtual}");
        GUI.Label(new Rect(15, 145, 240, 20), $"Velocidade: {(velocidadeAtual * 3.6f):F1} / {(modoAtual == ModosVelocidade.Lento ? 3 : 6)} km/h");
        string direcaoSimples = tipoDirecaoAtual.Contains("Traseira") ? "Traseira" : "Frontal";
        GUI.Label(new Rect(15, 165, 240, 20), $"Direção: {direcaoSimples}");

        // Estado (usa sistema de colisão)
        string estado = "Normal";
        if (sistemaColisao.EstaDeslizandoParede) estado = "Deslizar";
        else if (sistemaColisao.EstaEmColisao || sistemaColisao.EstaBloqueadoFrente || sistemaColisao.EstaBloqueadoTras)
            estado = "Colisão";

        // Amarelo para deslizar, vermelho para colisão, verde para normal
        if (sistemaColisao.EstaDeslizandoParede) GUI.color = Color.yellow;
        else if (sistemaColisao.EstaEmColisao || sistemaColisao.EstaBloqueadoFrente || sistemaColisao.EstaBloqueadoTras)
            GUI.color = Color.red;
        else GUI.color = Color.green;

        GUI.Label(new Rect(15, 185, 240, 20), $"Estado: {estado}");
        GUI.color = Color.white;

        // Travão de emergência 
        if (travaoDeEmergencia)
        {
            GUI.color = new Color(1, 0, 0, 0.9f);
            GUI.Box(new Rect(10, 220, 250, 35), "");
            GUI.color = Color.red;
            GUI.Label(new Rect(15, 228, 240, 20), "TRAVÃO DE EMERGÊNCIA ATIVO!");
            GUI.color = Color.white;
        }

        // Controlos
        int yPosControlos = travaoDeEmergencia ? 265 : 220;

        GUI.color = new Color(0, 0.5f, 0, 0.8f);
        GUI.Box(new Rect(10, yPosControlos, 250, 95), "");
        GUI.color = Color.white;
        GUI.Label(new Rect(15, yPosControlos + 5, 240, 20), "=== CONTROLOS ===");
        GUI.Label(new Rect(15, yPosControlos + 25, 240, 20), "WASD/Setas - Mover");
        GUI.Label(new Rect(15, yPosControlos + 42, 240, 20), "1/2 - Modo Lento/Normal");
        GUI.Label(new Rect(15, yPosControlos + 59, 240, 20), "T - Alternar direção");
        GUI.Label(new Rect(15, yPosControlos + 76, 240, 20), "ESPAÇO - Travão");
    }
}