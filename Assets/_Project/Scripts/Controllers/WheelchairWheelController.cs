using UnityEngine;

/// <summary>
/// Sistema completo de controlo das rodas da cadeira de rodas
/// Gere viragem (steering), rotação (spinning) e movimento diferencial das rodas
/// Suporta dois modos: Direção Frontal (standard) e Direção Traseira (mais manobrável)
/// </summary>
public class WheelchairWheelController : MonoBehaviour
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
    private WheelchairMovement movementScript;  // Script que move a cadeira
    private Rigidbody rb;                       // Rigidbody para física

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
        movementScript = GetComponent<WheelchairMovement>();
        rb = GetComponent<Rigidbody>();
        posicaoAnterior = transform.position;

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

        // Mensagens de debug para confirmar configuração
        Debug.Log($"🦽 Cadeira de Rodas - Modo: {tipoDirecao}");
        Debug.Log($"   Tecla {teclaAlternarDirecao} para alternar tipo de direção");
    }

    // ========================================================================
    // PROCURAR JOINTS AUTOMATICAMENTE - Encontra os bones por nome
    // ========================================================================
    
    /// <summary>
    /// Percorre toda a hierarquia de filhos procurando os joints por nome
    /// Isto evita ter que arrastar manualmente no Inspector
    /// </summary>
    void ProcurarJointsAutomaticamente()
    {
        // Obter todos os Transforms filhos (incluindo netos, bisnetos, etc)
        Transform[] todosTransforms = GetComponentsInChildren<Transform>();

        // Percorrer cada Transform procurando pelos nomes corretos
        foreach (Transform t in todosTransforms)
        {
            // Switch é mais eficiente que múltiplos if/else
            switch (t.name)
            {
                case "joint4":
                    joint4_ViragemFrontal = t;
                    Debug.Log("✅ joint4 (Viragem Frontal) encontrado!");
                    break;
                case "joint5":
                    joint5_ViragemTraseira = t;
                    Debug.Log("✅ joint5 (Viragem Traseira) encontrado!");
                    break;
                case "joint6":
                    joint6_RodaFrontalEsquerda = t;
                    Debug.Log("✅ joint6 (Roda Frontal Esquerda) encontrado!");
                    break;
                case "joint7":
                    joint7_RodaFrontalDireita = t;
                    Debug.Log("✅ joint7 (Roda Frontal Direita) encontrado!");
                    break;
                case "joint8":
                    joint8_RodaTraseiraEsquerda = t;
                    Debug.Log("✅ joint8 (Roda Traseira Esquerda) encontrado!");
                    break;
                case "joint9":
                    joint9_RodaTraseiraDireita = t;
                    Debug.Log("✅ joint9 (Roda Traseira Direita) encontrado!");
                    break;
            }
        }
    }

    // ========================================================================
    // GUARDAR ROTAÇÕES INICIAIS - Memoriza a posição neutra de cada joint
    // ========================================================================
    
    /// <summary>
    /// Guarda a rotação inicial de cada joint
    /// Isto serve como ponto de referência para aplicar rotações depois
    /// Exemplo: se queremos virar 30°, fazemos rotInicial * rotacao30graus
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

    // ========================================================================
    // VERIFICAR CONFIGURAÇÃO - Confirma que tudo está bem configurado
    // ========================================================================
    
    /// <summary>
    /// Verifica se os joints essenciais foram encontrados
    /// Mostra avisos no console se algo estiver em falta
    /// </summary>
    void VerificarConfiguracao()
    {
        bool tudoOk = true;

        // Verificar joints de viragem (essenciais)
        if (joint4_ViragemFrontal == null)
        {
            Debug.LogWarning("⚠️ joint4 (Viragem Frontal) não encontrado!");
            tudoOk = false;
        }

        if (joint5_ViragemTraseira == null)
        {
            Debug.LogWarning("⚠️ joint5 (Viragem Traseira) não encontrado!");
            tudoOk = false;
        }

        if (tudoOk)
        {
            Debug.Log("✅ Sistema de viragem configurado!");
        }

        // Contar quantas rodas foram encontradas
        int rodasConfiguradas = 0;
        if (joint6_RodaFrontalEsquerda != null) rodasConfiguradas++;
        if (joint7_RodaFrontalDireita != null) rodasConfiguradas++;
        if (joint8_RodaTraseiraEsquerda != null) rodasConfiguradas++;
        if (joint9_RodaTraseiraDireita != null) rodasConfiguradas++;

        Debug.Log($"🎮 {rodasConfiguradas}/4 rodas configuradas para rotação");
    }

    // ========================================================================
    // UPDATE - Loop principal executado a cada frame
    // ========================================================================
    
    void Update()
    {
        // Verificar se o jogador premiu a tecla para alternar tipo de direção
        if (Input.GetKeyDown(teclaAlternarDirecao))
        {
            AlternarTipoDirecao();
        }

        // Obter velocidade atual e input de viragem
        ObterVelocidadeEDirecao();

        // Verificar se está em movimento (útil para outras lógicas)
        // Mathf.Abs transforma negativos em positivos para comparação
        estaEmMovimento = Mathf.Abs(velocidadeAtual) > 0.01f || Mathf.Abs(inputViragem) > 0.01f;

        // Aplicar viragem às rodas apropriadas (frente ou trás)
        AplicarViragem();

        // Aplicar rotação às rodas (fazer girar como se estivessem a rolar)
        AplicarRotacaoRodas();
    }

    // ========================================================================
    // ALTERNAR TIPO DE DIREÇÃO - Muda entre frontal e traseira
    // ========================================================================
    
    /// <summary>
    /// Alterna entre Direção Frontal e Direção Traseira
    /// Cada modo tem características diferentes de manobrabilidade
    /// </summary>
    void AlternarTipoDirecao()
    {
        if (tipoDirecao == TipoDirecao.DirecaoFrontal)
        {
            // Mudar para Direção Traseira
            tipoDirecao = TipoDirecao.DirecaoTraseira;
            Debug.Log("🦽 Direção Traseira - Rodas de trás viram (mais manobrável)");

            // Ajustar características de movimento para este modo
            if (movementScript != null)
            {
                movementScript.velocidadeRotacao = 60f;  // Vira mais rápido
                movementScript.rotacaoNoLugar = true;    // Pode girar sem avançar
            }
        }
        else
        {
            // Mudar para Direção Frontal
            tipoDirecao = TipoDirecao.DirecaoFrontal;
            Debug.Log("🦽 Direção Frontal - Rodas da frente viram (standard)");

            // Ajustar características de movimento para este modo
            if (movementScript != null)
            {
                movementScript.velocidadeRotacao = 45f;  // Vira mais devagar
                movementScript.rotacaoNoLugar = false;   // Precisa de espaço para virar
            }
        }

        // Reset das posições de viragem ao trocar de modo
        ResetarViragem();
    }

    // ========================================================================
    // OBTER VELOCIDADE E DIREÇÃO - Descobre quão rápido e para onde vai
    // ========================================================================
    
    /// <summary>
    /// Tenta obter velocidade e direção de 3 formas diferentes:
    /// 1. Do script WheelchairMovement (preferencial)
    /// 2. Do Rigidbody
    /// 3. Calculando manualmente a partir da posição
    /// </summary>
    void ObterVelocidadeEDirecao()
    {
        // MÉTODO 1: Usar o script de movimento (mais fiável)
        if (movementScript != null)
        {
            velocidadeAtual = movementScript.GetVelocidadeNormalizada();
            inputViragem = Input.GetAxis("Horizontal");
        }
        // MÉTODO 2: Usar Rigidbody (se não tiver o script)
        else if (rb != null)
        {
            // Vector3.Dot calcula quanto da velocidade está na direção frontal
            float velocidadeFrontal = Vector3.Dot(rb.linearVelocity, transform.forward);
            
            // Normalizar dividindo pela velocidade máxima
            velocidadeAtual = velocidadeFrontal / (velocidadeMaximaKmH / 3.6f);  // km/h para m/s
            
            // Usar velocidade angular para detectar viragem
            inputViragem = rb.angularVelocity.y / 2f;
        }
        // MÉTODO 3: Calcular manualmente (fallback)
        else
        {
            // Calcular movimento desde o último frame
            Vector3 movimento = transform.position - posicaoAnterior;
            
            // Calcular velocidade frontal
            float velocidadeFrontal = Vector3.Dot(movimento / Time.deltaTime, transform.forward);
            
            // Normalizar
            velocidadeAtual = velocidadeFrontal / (velocidadeMaximaKmH / 3.6f);
            
            // Input de viragem vem diretamente do teclado
            inputViragem = Input.GetAxis("Horizontal");
            
            // Guardar posição para o próximo frame
            posicaoAnterior = transform.position;
        }

        // Limitar valores entre -1 e 1 (normalizado)
        velocidadeAtual = Mathf.Clamp(velocidadeAtual, -1f, 1f);
        inputViragem = Mathf.Clamp(inputViragem, -1f, 1f);
    }

    // ========================================================================
    // APLICAR VIRAGEM - Faz as rodas virarem para a esquerda/direita
    // ========================================================================
    
    /// <summary>
    /// Aplica viragem (steering) às rodas corretas baseado no tipo de direção
    /// DirecaoFrontal: apenas rodas da frente viram
    /// DirecaoTraseira: apenas rodas de trás viram
    /// </summary>
    void AplicarViragem()
    {
        // Calcular ângulo de viragem desejado
        // inputViragem vai de -1 (esquerda) a +1 (direita)
        // Multiplica pelo ângulo máximo para obter o ângulo final
        float anguloAlvo = inputViragem * anguloMaximoViragem;

        // Suavizar a viragem usando Lerp (interpolação linear)
        // Isto faz a viragem ser gradual em vez de instantânea
        anguloViragemAtual = Mathf.Lerp(
            anguloViragemAtual,    // Onde estamos
            anguloAlvo,            // Para onde queremos ir
            velocidadeViragem * Time.deltaTime  // Quão rápido vamos
        );

        // Aplicar viragem baseada no tipo de direção selecionado
        if (tipoDirecao == TipoDirecao.DirecaoFrontal)
        {
            // MODO FRONTAL: Só as rodas da frente viram
            if (joint4_ViragemFrontal != null)
            {
                // Criar rotação no eixo Y (viragem horizontal)
                Quaternion rotacaoViragem = Quaternion.AngleAxis(anguloViragemAtual, EIXO_VIRAGEM);
                
                // Aplicar rotação mantendo a rotação inicial como base
                joint4_ViragemFrontal.localRotation = rotInicialJoint4 * rotacaoViragem;
            }

            // Rodas traseiras permanecem retas (voltar à posição inicial)
            if (joint5_ViragemTraseira != null)
            {
                joint5_ViragemTraseira.localRotation = rotInicialJoint5;
            }
        }
        else // TipoDirecao.DirecaoTraseira
        {
            // MODO TRASEIRO: Só as rodas de trás viram
            if (joint5_ViragemTraseira != null)
            {
                // Criar rotação no eixo Y
                Quaternion rotacaoViragem = Quaternion.AngleAxis(anguloViragemAtual, EIXO_VIRAGEM);
                
                // Aplicar rotação
                joint5_ViragemTraseira.localRotation = rotInicialJoint5 * rotacaoViragem;
            }

            // Rodas frontais permanecem retas
            if (joint4_ViragemFrontal != null)
            {
                joint4_ViragemFrontal.localRotation = rotInicialJoint4;
            }
        }
    }

    // ========================================================================
    // APLICAR ROTAÇÃO DAS RODAS - Faz as rodas girarem como se rolassem
    // ========================================================================
    
    /// <summary>
    /// Calcula e aplica a rotação visual das rodas baseado em física real
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

        Debug.Log(" Todas as rodas paradas e resetadas!");
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