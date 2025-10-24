using UnityEngine;

/// <summary>
/// Sistema completo de controlo das rodas da cadeira de rodas
/// Gere viragem (steering), rotação (spinning) e movimento diferencial das rodas
/// Suporta dois modos: Direção Frontal (standard) e Direção Traseira (mais manobrável)
/// </summary>
public class WheelController : MonoBehaviour
{
    // ========================================================================
    // JOINTS DE VIRAGEM (STEERING) - Controlam a direção das rodas
    // ========================================================================
    
    [Header("=== Joints de Viragem (Steering) ===")]
    
    [Tooltip("Joint central das rodas frontais - controla viragem")]
    public Transform joint4_ViragemFrontal;  // Bone que vira as rodas da frente

    [Tooltip("Joint central das rodas traseiras - controla viragem")]
    public Transform joint5_ViragemTraseira;  // Bone que vira as rodas de trás

    // ========================================================================
    // JOINTS DE ROTAÇÃO DAS RODAS - Fazem as rodas girarem (spinning)
    // ========================================================================
    
    [Header("=== Joints de Rotação das Rodas ===")]
    
    [Tooltip("Joint da roda frontal esquerda - gira a roda")]
    public Transform joint6_RodaFrontalEsquerda;

    [Tooltip("Joint da roda frontal direita - gira a roda")]
    public Transform joint7_RodaFrontalDireita;

    [Tooltip("Joint da roda traseira esquerda - gira a roda")]
    public Transform joint8_RodaTraseiraEsquerda;

    [Tooltip("Joint da roda traseira direita - gira a roda")]
    public Transform joint9_RodaTraseiraDireita;

    // ========================================================================
    // TIPO DE CADEIRA DE RODAS - Define qual conjunto de rodas vira
    // ========================================================================
    
    [Header("=== Tipo de Cadeira de Rodas ===")]
    
    [Tooltip("Tipo de direção da cadeira")]
    public TipoDirecao tipoDirecao = TipoDirecao.DirecaoFrontal;

    [Tooltip("Tecla para alternar tipo de direção")]
    public KeyCode teclaAlternarDirecao = KeyCode.T;

    // ========================================================================
    // CONFIGURAÇÃO FÍSICA - Parâmetros reais da cadeira
    // ========================================================================
    
    [Header("=== Configuração Física ===")]
    
    [Tooltip("Velocidade máxima da cadeira em km/h")]
    public float velocidadeMaximaKmH = 6f;  // Velocidade típica de cadeira elétrica

    [Tooltip("Diâmetro das rodas traseiras em metros")]
    public float diametroRodasTraseiras = 0.6f;  // 60cm = rodas grandes

    [Tooltip("Diâmetro das rodas frontais em metros")]
    public float diametroRodasFrontais = 0.15f;  // 15cm = rodas pequenas

    [Tooltip("Multiplicador de velocidade de rotação")]
    public float multiplicadorVelocidade = 5f;  // Ajuste fino da rotação visual

    // ========================================================================
    // CONFIGURAÇÃO DE VIRAGEM (STEERING) - Como a cadeira vira
    // ========================================================================
    
    [Header("=== Configuração de Viragem ===")]
    
    [Tooltip("Ângulo máximo de viragem")]
    [Range(0f, 45f)]
    public float anguloMaximoViragem = 30f;  // Máximo que as rodas podem virar

    [Tooltip("Velocidade de viragem")]
    [Range(1f, 10f)]
    public float velocidadeViragem = 5f;  // Quão rápido as rodas viram

    // ========================================================================
    // CONFIGURAÇÃO DE ROTAÇÃO DIFERENCIAL - Rodas internas/externas em curvas
    // ========================================================================
    
    [Header("=== Configuração de Rotação ===")]
    
    [Tooltip("Fazer rodas girarem de forma diferencial nas curvas")]
    public bool rotacaoDiferencial = true;  // Simula comportamento realista

    [Tooltip("Intensidade da rotação diferencial")]
    [Range(0f, 2f)]
    public float intensidadeDiferencial = 0.5f;  // Diferença entre roda interna/externa

    [Tooltip("Inverter direção de rotação")]
    public bool inverterRotacao = false;  // Se as rodas estão a girar ao contrário

    // ========================================================================
    // DEBUG INFO - Valores visíveis no Inspector para debugging
    // ========================================================================
    
    [Header("=== Debug Info ===")]
    [SerializeField] private float rotacaoRodaFrontalEsq = 0f;    // Rotação acumulada
    [SerializeField] private float rotacaoRodaFrontalDir = 0f;
    [SerializeField] private float rotacaoRodaTraseiraEsq = 0f;
    [SerializeField] private float rotacaoRodaTraseiraDir = 0f;
    [SerializeField] private float anguloViragemAtual = 0f;       // Ângulo atual de viragem
    [SerializeField] private float velocidadeAtual = 0f;          // Velocidade normalizada (-1 a 1)
    [SerializeField] private float inputViragem = 0f;             // Input de viragem (-1 a 1)
    [SerializeField] private bool estaEmMovimento = false;        // Se a cadeira está a mover-se

    // ========================================================================
    // ENUM - Tipos de direção disponíveis
    // ========================================================================
    
    /// <summary>
    /// Define qual conjunto de rodas controla a direção
    /// DirecaoFrontal: Como um carro normal (rodas da frente viram)
    /// DirecaoTraseira: Mais manobrável, como empilhador (rodas de trás viram)
    /// </summary>
    public enum TipoDirecao
    {
        DirecaoFrontal,    // Rodas da frente viram (cadeira standard)
        DirecaoTraseira    // Rodas de trás viram (cadeira mais manobrável)
    }

    // ========================================================================
    // VARIÁVEIS PRIVADAS - Referências e estado interno
    // ========================================================================
    
    // Referências a outros componentes
    private Movement movementScript;  // Script que move a cadeira
    private Rigidbody rb;                       // Rigidbody para física
    private Sounds wheelchairSounds;  // Sistema de sons da cadeira

    // Rotações iniciais de cada joint (para poder voltar à posição neutra)
    private Quaternion rotInicialJoint4;
    private Quaternion rotInicialJoint5;
    private Quaternion rotInicialJoint6;
    private Quaternion rotInicialJoint7;
    private Quaternion rotInicialJoint8;
    private Quaternion rotInicialJoint9;

    // Para calcular velocidade manualmente se necessário
    private Vector3 posicaoAnterior;

    // Eixos de rotação (constantes)
    // readonly = valor não muda depois de ser definido
    private readonly Vector3 EIXO_ROTACAO = Vector3.forward;  // Z para girar as rodas
    private readonly Vector3 EIXO_VIRAGEM = Vector3.up;       // Y para virar (steering)

    // ========================================================================
    // START - Inicialização quando o jogo começa
    // ========================================================================
    
    void Start()
    {
        // Obter referências aos componentes necessários
        movementScript = GetComponent<Movement>();
        rb = GetComponent<Rigidbody>();
        posicaoAnterior = transform.position;

        // === OBTER REFERÊNCIA AO SISTEMA DE SONS (SEGURO) ===
        // Tentar encontrar o WheelchairSounds em vários locais
        wheelchairSounds = GetComponentInChildren<Sounds>();
        
        if (wheelchairSounds == null && transform.parent != null)
        {
            wheelchairSounds = transform.parent.GetComponentInChildren<Sounds>();
        }
        
        if (wheelchairSounds == null)
        {
            wheelchairSounds = GetComponentInParent<Sounds>();
        }
        
        if (wheelchairSounds == null)
        {
            wheelchairSounds = FindObjectOfType<Sounds>();
        }
        
        if (wheelchairSounds == null)
        {
            Debug.LogWarning("⚠️ WheelchairSounds não encontrado no WheelController! Som de clique não vai funcionar ao mudar direção.");
        }
        else
        {
            Debug.Log("✅ WheelchairSounds encontrado no WheelController!");
        }

        // Procurar automaticamente todos os joints na hierarquia
        ProcurarJointsAutomaticamente();

        // Guardar as rotações iniciais de cada joint (posição neutra)
        GuardarRotacoesIniciais();

        // Verificar se tudo está configurado corretamente
        VerificarConfiguracao();

        // Ajustar comportamento do movimento baseado no tipo de direção
        if (movementScript != null)
        {
            if (tipoDirecao == TipoDirecao.DirecaoTraseira)
            {
                // Direção traseira = mais ágil
                movementScript.velocidadeRotacao = 60f;
                movementScript.rotacaoNoLugar = true;  // Pode girar sem avançar
            }
            else
            {
                // Direção frontal = standard
                movementScript.velocidadeRotacao = 45f;
                movementScript.rotacaoNoLugar = false;  // Precisa de avançar para virar
            }
        }

        Debug.Log("✅ WheelController inicializado!");
        Debug.Log($"📐 Modo: {tipoDirecao}");
    }

    // ========================================================================
    // UPDATE - Executado a cada frame
    // ========================================================================
    
    void Update()
    {
        // Alternar tipo de direção se pressionar a tecla (default: T)
        if (Input.GetKeyDown(teclaAlternarDirecao))
        {
            AlternarTipoDirecao();
        }

        // Obter inputs do jogador
        ObterInputs();

        // Aplicar viragem (steering) baseado no tipo de direção
        AplicarViragem();

        // Girar as rodas baseado na velocidade
        AplicarRotacaoRodas();
    }

    // ========================================================================
    // ALTERNAR TIPO DE DIREÇÃO - Muda entre frontal/traseira
    // ========================================================================
    
    /// <summary>
    /// Alterna entre direção frontal e traseira
    /// Também ajusta comportamento do movimento automaticamente
    /// </summary>
    void AlternarTipoDirecao()
    {
        // Mudar o tipo
        if (tipoDirecao == TipoDirecao.DirecaoFrontal)
        {
            tipoDirecao = TipoDirecao.DirecaoTraseira;
            Debug.Log("🔄 Mudou para: DIREÇÃO TRASEIRA (mais manobrável)");
        }
        else
        {
            tipoDirecao = TipoDirecao.DirecaoFrontal;
            Debug.Log("🔄 Mudou para: DIREÇÃO FRONTAL (standard)");
        }

        // === Tocar som de clique (SEGURO) ===
        if (wheelchairSounds != null)
        {
            wheelchairSounds.TocarClique();
        }

        // Resetar viragem ao mudar de modo (voltar rodas a retas)
        ResetarViragem();

        // Ajustar comportamento do movimento
        if (movementScript != null)
        {
            if (tipoDirecao == TipoDirecao.DirecaoTraseira)
            {
                // Direção traseira = mais ágil, raio de viragem menor
                movementScript.velocidadeRotacao = 60f;
                movementScript.rotacaoNoLugar = true;
            }
            else
            {
                // Direção frontal = comportamento standard
                movementScript.velocidadeRotacao = 45f;
                movementScript.rotacaoNoLugar = false;
            }
        }
    }

    // ========================================================================
    // OBTER INPUTS - Lê inputs do jogador
    // ========================================================================
    
    /// <summary>
    /// Obtém os inputs do jogador e calcula velocidade atual
    /// Usa o script Movement se disponível, senão calcula manualmente
    /// </summary>
    void ObterInputs()
    {
        // Input de viragem (A/D ou Setas Esquerda/Direita)
        inputViragem = Input.GetAxis("Horizontal");

        // Calcular velocidade atual
        if (movementScript != null)
        {
            // Usar método do Movement para obter velocidade normalizada
            velocidadeAtual = movementScript.GetVelocidadeNormalizada();
            estaEmMovimento = movementScript.EstaEmMovimento();
        }
        else if (rb != null)
        {
            // Fallback: calcular velocidade manualmente usando Rigidbody
            velocidadeAtual = rb.linearVelocity.magnitude / (velocidadeMaximaKmH / 3.6f);
            velocidadeAtual = Mathf.Clamp(velocidadeAtual, -1f, 1f);
            estaEmMovimento = rb.linearVelocity.magnitude > 0.1f;
        }
        else
        {
            // Último recurso: calcular pela mudança de posição
            float distancia = Vector3.Distance(transform.position, posicaoAnterior);
            float velocidadeCalculada = distancia / Time.deltaTime;
            velocidadeAtual = velocidadeCalculada / (velocidadeMaximaKmH / 3.6f);
            velocidadeAtual = Mathf.Clamp(velocidadeAtual, -1f, 1f);
            estaEmMovimento = distancia > 0.01f;

            posicaoAnterior = transform.position;
        }
    }

    // ========================================================================
    // APLICAR VIRAGEM - Vira as rodas baseado no input
    // ========================================================================
    
    /// <summary>
    /// Aplica viragem (steering) às rodas corretas dependendo do modo
    /// DirecaoFrontal: Vira rodas da frente
    /// DirecaoTraseira: Vira rodas de trás
    /// </summary>
    void AplicarViragem()
    {
        // Só virar se houver input de viragem
        if (Mathf.Abs(inputViragem) > 0.01f)
        {
            // Calcular ângulo alvo baseado no input
            float anguloAlvo = inputViragem * anguloMaximoViragem;

            // Interpolar suavemente até ao ângulo alvo
            anguloViragemAtual = Mathf.Lerp(
                anguloViragemAtual,
                anguloAlvo,
                velocidadeViragem * Time.deltaTime
            );
        }
        else
        {
            // Se não há input, voltar suavemente para 0 (retas)
            anguloViragemAtual = Mathf.Lerp(
                anguloViragemAtual,
                0f,
                velocidadeViragem * Time.deltaTime
            );
        }

        // Criar rotação baseada no ângulo calculado
        Quaternion rotacaoViragem = Quaternion.AngleAxis(anguloViragemAtual, EIXO_VIRAGEM);

        // Aplicar viragem ao joint correto dependendo do modo
        if (tipoDirecao == TipoDirecao.DirecaoFrontal)
        {
            // Modo FRONTAL: Virar rodas da FRENTE
            if (joint4_ViragemFrontal != null)
            {
                joint4_ViragemFrontal.localRotation = rotInicialJoint4 * rotacaoViragem;
            }

            // Garantir que rodas traseiras estão retas
            if (joint5_ViragemTraseira != null)
            {
                joint5_ViragemTraseira.localRotation = rotInicialJoint5;
            }
        }
        else
        {
            // Modo TRASEIRO: Virar rodas de TRÁS
            if (joint5_ViragemTraseira != null)
            {
                joint5_ViragemTraseira.localRotation = rotInicialJoint5 * rotacaoViragem;
            }

            // Garantir que rodas frontais estão retas
            if (joint4_ViragemFrontal != null)
            {
                joint4_ViragemFrontal.localRotation = rotInicialJoint4;
            }
        }
    }

    // ========================================================================
    // APLICAR ROTAÇÃO DAS RODAS - Faz as rodas girarem baseado na velocidade
    // ========================================================================
    
    /// <summary>
    /// Calcula e aplica rotação realista a todas as rodas
    /// Usa o diâmetro das rodas e velocidade para calcular RPM correto
    /// Implementa rotação diferencial para curvas mais realistas
    /// </summary>
    void AplicarRotacaoRodas()
    {
        // ===  CÁLCULO DE ROTAÇÃO DAS RODAS TRASEIRAS ===
        
        // Circunferência = π × diâmetro (perímetro da roda)
        float circunferenciaTraseira = Mathf.PI * diametroRodasTraseiras;
        
        // Quantas rotações completas por metro percorrido
        // Se circunferência = 2m, então 1 rotação = 2m, logo 0.5 rotações por metro
        float rotacoesPorMetroTraseira = 1f / circunferenciaTraseira;
        
        // Converter velocidade de km/h para m/s (dividir por 3.6)
        // velocidadeAtual é normalizada (-1 a 1), multiplicamos pela velocidade máxima
        float velocidadeMetrosPorSegundo = velocidadeAtual * (velocidadeMaximaKmH / 3.6f);
        
        // Calcular rotações por segundo
        float rotacoesPorSegundoTraseira = velocidadeMetrosPorSegundo * rotacoesPorMetroTraseira;
        
        // Converter para graus por segundo (1 rotação = 360 graus)
        // Multiplicar pelo multiplicador para ajuste visual
        float grausPorSegundoTraseira = rotacoesPorSegundoTraseira * 360f * multiplicadorVelocidade;

        // === CÁLCULO DE ROTAÇÃO DAS RODAS FRONTAIS ===
        // Mesmo processo mas com diâmetro diferente
        
        float circunferenciaFrontal = Mathf.PI * diametroRodasFrontais;
        float rotacoesPorMetroFrontal = 1f / circunferenciaFrontal;
        float rotacoesPorSegundoFrontal = velocidadeMetrosPorSegundo * rotacoesPorMetroFrontal;
        float grausPorSegundoFrontal = rotacoesPorSegundoFrontal * 360f * multiplicadorVelocidade;

        // Inverter rotação se necessário (caso as rodas estejam ao contrário)
        if (inverterRotacao)
        {
            grausPorSegundoTraseira = -grausPorSegundoTraseira;
            grausPorSegundoFrontal = -grausPorSegundoFrontal;
        }

        // === ROTAÇÃO DIFERENCIAL ===
        // Em curvas, a roda externa gira mais rápido que a interna
        
        float deltaRotacaoEsquerda = 1f;  // Multiplicador da roda esquerda
        float deltaRotacaoDireita = 1f;   // Multiplicador da roda direita

        // Só aplicar diferencial se ativado E se estiver a virar
        if (rotacaoDiferencial && Mathf.Abs(inputViragem) > 0.01f)
        {
            // Intensidade base do diferencial
            float intensidade = intensidadeDiferencial;

            // Direção traseira tem diferencial mais agressivo (mais manobrável)
            if (tipoDirecao == TipoDirecao.DirecaoTraseira)
            {
                intensidade *= 1.5f;
            }

            if (inputViragem > 0)  // Virando para a DIREITA
            {
                // Roda ESQUERDA (externa) gira mais rápido
                deltaRotacaoEsquerda = 1f + (Mathf.Abs(inputViragem) * intensidade);
                
                // Roda DIREITA (interna) gira mais devagar
                deltaRotacaoDireita = 1f - (Mathf.Abs(inputViragem) * intensidade * 0.5f);
            }
            else  // Virando para a ESQUERDA
            {
                // Roda DIREITA (externa) gira mais rápido
                deltaRotacaoDireita = 1f + (Mathf.Abs(inputViragem) * intensidade);
                
                // Roda ESQUERDA (interna) gira mais devagar
                deltaRotacaoEsquerda = 1f - (Mathf.Abs(inputViragem) * intensidade * 0.5f);
            }
        }

        // === ATUALIZAR ROTAÇÕES ACUMULADAS ===
        // Acumular a rotação ao longo do tempo (+=)
        // Time.deltaTime garante que funciona igual em qualquer framerate
        
        rotacaoRodaTraseiraEsq += grausPorSegundoTraseira * deltaRotacaoEsquerda * Time.deltaTime;
        rotacaoRodaTraseiraDir += grausPorSegundoTraseira * deltaRotacaoDireita * Time.deltaTime;
        rotacaoRodaFrontalEsq += grausPorSegundoFrontal * deltaRotacaoEsquerda * Time.deltaTime;
        rotacaoRodaFrontalDir += grausPorSegundoFrontal * deltaRotacaoDireita * Time.deltaTime;

        // === APLICAR ROTAÇÕES AOS JOINTS ===
        // Criar Quaternion para cada roda e aplicar
        
        // Roda Traseira Esquerda
        if (joint8_RodaTraseiraEsquerda != null)
        {
            Quaternion rotacao = Quaternion.AngleAxis(rotacaoRodaTraseiraEsq, EIXO_ROTACAO);
            joint8_RodaTraseiraEsquerda.localRotation = rotInicialJoint8 * rotacao;
        }

        // Roda Traseira Direita
        if (joint9_RodaTraseiraDireita != null)
        {
            Quaternion rotacao = Quaternion.AngleAxis(rotacaoRodaTraseiraDir, EIXO_ROTACAO);
            joint9_RodaTraseiraDireita.localRotation = rotInicialJoint9 * rotacao;
        }

        // Roda Frontal Esquerda
        if (joint6_RodaFrontalEsquerda != null)
        {
            Quaternion rotacao = Quaternion.AngleAxis(rotacaoRodaFrontalEsq, EIXO_ROTACAO);
            joint6_RodaFrontalEsquerda.localRotation = rotInicialJoint6 * rotacao;
        }

        // Roda Frontal Direita
        if (joint7_RodaFrontalDireita != null)
        {
            Quaternion rotacao = Quaternion.AngleAxis(rotacaoRodaFrontalDir, EIXO_ROTACAO);
            joint7_RodaFrontalDireita.localRotation = rotInicialJoint7 * rotacao;
        }
    }

    // ========================================================================
    // RESETAR VIRAGEM - Volta as rodas para a posição neutra (retas)
    // ========================================================================
    
    /// <summary>
    /// Volta as rodas para a posição reta (sem viragem)
    /// Útil quando se muda de modo de direção
    /// </summary>
    void ResetarViragem()
    {
        // Zerar ângulo atual
        anguloViragemAtual = 0f;

        // Voltar joints de viragem às rotações iniciais (posição neutra)
        if (joint4_ViragemFrontal != null)
            joint4_ViragemFrontal.localRotation = rotInicialJoint4;

        if (joint5_ViragemTraseira != null)
            joint5_ViragemTraseira.localRotation = rotInicialJoint5;
    }

    // ========================================================================
    // MÉTODOS AUXILIARES - Funções de suporte
    // ========================================================================
    
    /// <summary>
    /// Procura automaticamente todos os joints na hierarquia do GameObject
    /// </summary>
    void ProcurarJointsAutomaticamente()
    {
        // Procurar joints de viragem
        if (joint4_ViragemFrontal == null)
            joint4_ViragemFrontal = transform.Find("joint4");
        if (joint5_ViragemTraseira == null)
            joint5_ViragemTraseira = transform.Find("joint5");

        // Procurar joints de rotação
        if (joint6_RodaFrontalEsquerda == null)
            joint6_RodaFrontalEsquerda = transform.Find("joint6");
        if (joint7_RodaFrontalDireita == null)
            joint7_RodaFrontalDireita = transform.Find("joint7");
        if (joint8_RodaTraseiraEsquerda == null)
            joint8_RodaTraseiraEsquerda = transform.Find("joint8");
        if (joint9_RodaTraseiraDireita == null)
            joint9_RodaTraseiraDireita = transform.Find("joint9");
    }

    /// <summary>
    /// Guarda as rotações iniciais de todos os joints
    /// </summary>
    void GuardarRotacoesIniciais()
    {
        if (joint4_ViragemFrontal != null)
            rotInicialJoint4 = joint4_ViragemFrontal.localRotation;
        if (joint5_ViragemTraseira != null)
            rotInicialJoint5 = joint5_ViragemTraseira.localRotation;
        if (joint6_RodaFrontalEsquerda != null)
            rotInicialJoint6 = joint6_RodaFrontalEsquerda.localRotation;
        if (joint7_RodaFrontalDireita != null)
            rotInicialJoint7 = joint7_RodaFrontalDireita.localRotation;
        if (joint8_RodaTraseiraEsquerda != null)
            rotInicialJoint8 = joint8_RodaTraseiraEsquerda.localRotation;
        if (joint9_RodaTraseiraDireita != null)
            rotInicialJoint9 = joint9_RodaTraseiraDireita.localRotation;
    }

    /// <summary>
    /// Verifica se todos os componentes necessários estão configurados
    /// </summary>
    void VerificarConfiguracao()
    {
        bool tudoOk = true;

        if (joint4_ViragemFrontal == null)
        {
            Debug.LogWarning("⚠️ joint4_ViragemFrontal não encontrado!");
            tudoOk = false;
        }
        if (joint5_ViragemTraseira == null)
        {
            Debug.LogWarning("⚠️ joint5_ViragemTraseira não encontrado!");
            tudoOk = false;
        }
        if (joint6_RodaFrontalEsquerda == null)
        {
            Debug.LogWarning("⚠️ joint6_RodaFrontalEsquerda não encontrado!");
            tudoOk = false;
        }
        if (joint7_RodaFrontalDireita == null)
        {
            Debug.LogWarning("⚠️ joint7_RodaFrontalDireita não encontrado!");
            tudoOk = false;
        }
        if (joint8_RodaTraseiraEsquerda == null)
        {
            Debug.LogWarning("⚠️ joint8_RodaTraseiraEsquerda não encontrado!");
            tudoOk = false;
        }
        if (joint9_RodaTraseiraDireita == null)
        {
            Debug.LogWarning("⚠️ joint9_RodaTraseiraDireita não encontrado!");
            tudoOk = false;
        }

        if (tudoOk)
        {
            Debug.Log("✅ Todos os joints encontrados!");
        }
    }

    // ========================================================================
    // MÉTODOS PÚBLICOS - Funções que outros scripts podem chamar
    // ========================================================================
    
    /// <summary>
    /// Para completamente todas as rodas e reseta para posição inicial
    /// Útil para teleportar a cadeira ou iniciar cutscenes
    /// </summary>
    public void PararRodas()
    {
        // Zerar todas as rotações acumuladas
        rotacaoRodaFrontalEsq = 0f;
        rotacaoRodaFrontalDir = 0f;
        rotacaoRodaTraseiraEsq = 0f;
        rotacaoRodaTraseiraDir = 0f;
        anguloViragemAtual = 0f;
        velocidadeAtual = 0f;
        inputViragem = 0f;

        // Resetar viragem
        ResetarViragem();

        // Voltar todas as rodas às rotações iniciais
        if (joint6_RodaFrontalEsquerda != null)
            joint6_RodaFrontalEsquerda.localRotation = rotInicialJoint6;

        if (joint7_RodaFrontalDireita != null)
            joint7_RodaFrontalDireita.localRotation = rotInicialJoint7;

        if (joint8_RodaTraseiraEsquerda != null)
            joint8_RodaTraseiraEsquerda.localRotation = rotInicialJoint8;

        if (joint9_RodaTraseiraDireita != null)
            joint9_RodaTraseiraDireita.localRotation = rotInicialJoint9;

        Debug.Log("🛑 Todas as rodas paradas e resetadas!");
    }

    /// <summary>
    /// Devolve o tipo de direção atual
    /// Útil para outros scripts saberem o modo ativo
    /// </summary>
    public TipoDirecao GetTipoDirecao()
    {
        return tipoDirecao;
    }
}