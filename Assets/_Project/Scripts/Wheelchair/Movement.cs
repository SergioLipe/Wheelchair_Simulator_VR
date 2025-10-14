using UnityEngine;
using System.Collections;
using System.Collections.Generic;



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

    [Header("=== Física e Limites ===")]
    [Tooltip("Inclinação máxima que consegue subir (graus)")]
    public float inclinacaoMaxima = 10f;

    [Tooltip("Gravidade aplicada")]
    public float gravidade = -9.81f;

    [Header("=== Sistema de Colisão ===")]
    [Tooltip("Ativar sistema de deteção de colisões")]
    public bool avisosColisaoAtivos = true;

    [Tooltip("Distância para deteção de obstáculos")]
    public float distanciaAviso = 1.5f;

    [Tooltip("Distância REAL para bloqueio (metros) - quão perto pode chegar")]
    [Range(0.01f, 0.2f)]
    public float distanciaBloqueio = 0.05f;  // 5cm do objeto - MUITO próximo!

    [Tooltip("Mostrar raios de debug na Scene View")]
    public bool mostrarDebugRaios = true;

    [Header("=== Estado Atual (Debug) ===")]
    [SerializeField] private float velocidadeAtual = 0f;
    [SerializeField] private float velocidadeDesejada = 0f;
    [SerializeField] private bool travaoDeEmergencia = false;
    [SerializeField] private string tipoDirecaoAtual = "Frontal";
    [SerializeField] private bool emColisao = false;
    [SerializeField] private string objetoColidido = "";
    [SerializeField] private float distanciaObstaculo = 999f;
    [SerializeField] private bool bloqueadoFrente = false;
    [SerializeField] private bool bloqueadoTras = false;
    [SerializeField] private float eficienciaRotacao = 100f;

    // Componentes
    private CharacterController controller;
    private Vector3 movimentoVelocidade;
    private WheelController wheelController;

    // Sistema de input suavizado
    private float inputVerticalSuavizado = 0f;
    private float inputHorizontalSuavizado = 0f;

    // Variáveis de colisão melhoradas
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

    // Direção traseira - feedback
    private bool tentandoVirarParado = false;
    private float tempoTentandoVirar = 0f;

    public enum ModosVelocidade
    {
        Lento,
        Normal,
        Desligado
    }

    void Start()
    {
        // Configurar o CharacterController com valores OTIMIZADOS
        controller = GetComponent<CharacterController>();
        if (controller == null)
        {
            controller = gameObject.AddComponent<CharacterController>();
        }

        // === VALORES OTIMIZADOS PARA COLISÃO PRECISA ===
        // Assumindo que a cadeira tem ~60cm de largura (escala 1x1x1)
        controller.height = 1.4f;
        controller.radius = 0.25f;  // REDUZIDO de 0.35f para 0.25f (25cm = mais preciso)
        controller.center = new Vector3(0, 0.7f, 0);

        // SkinWidth MÍNIMO possível
        controller.skinWidth = 0.005f;  // REDUZIDO de 0.01f para 0.005f
        controller.minMoveDistance = 0.0001f;  // Ainda mais sensível
        controller.stepOffset = 0.1f;

        // Elevar um pouco no início
        transform.position += Vector3.up * 0.1f;

        // Obter referência ao wheel controller
        wheelController = GetComponent<WheelController>();

        // Converter km/h para m/s
        velocidadeMaximaNormal = velocidadeMaximaNormal / 3.6f;
        velocidadeMaximaLenta = velocidadeMaximaLenta / 3.6f;
        velocidadeMarchaAtras = velocidadeMarchaAtras / 3.6f;

        Debug.Log("✅ WheelchairMovement - Colisões PRECISAS ativas!");
        Debug.Log($"📏 Radius: {controller.radius}m | SkinWidth: {controller.skinWidth}m");
        Debug.Log($"📏 Distância de bloqueio: {distanciaBloqueio}m ({distanciaBloqueio * 100}cm)");
    }

    void Update()
    {
        // Atualizar tipo de direção para debug
        if (wheelController != null)
        {
            tipoDirecaoAtual = wheelController.GetTipoDirecao().ToString();
        }

        // Verificar obstáculos
        if (avisosColisaoAtivos)
        {
            VerificarObstaculosCompleto();
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

        // Reset automático da colisão após 0.5 segundos
        if (emColisao && Time.time - tempoColisao > 0.5f)
        {
            emColisao = false;
            objetoColidido = "";
            deslizaParede = false;
        }
    }

    void GerirModos()
    {
        // Tecla 1: Modo Lento
        if (Input.GetKeyDown(KeyCode.Alpha1))
        {
            modoAtual = ModosVelocidade.Lento;
            Debug.Log("Modo: LENTO (Interior) - 3 km/h");
        }
        // Tecla 2: Modo Normal
        else if (Input.GetKeyDown(KeyCode.Alpha2))
        {
            modoAtual = ModosVelocidade.Normal;
            Debug.Log("Modo: NORMAL - 6 km/h");
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

        // Suavizar o input
        float suavizacao = 3f;
        inputVerticalSuavizado = Mathf.Lerp(inputVerticalSuavizado, inputVertical, suavizacao * Time.deltaTime);
        inputHorizontalSuavizado = Mathf.Lerp(inputHorizontalSuavizado, inputHorizontal, suavizacao * Time.deltaTime);

        // Determinar velocidade máxima baseada no modo
        float velocidadeMaxima = modoAtual == ModosVelocidade.Lento ?
                                velocidadeMaximaLenta : velocidadeMaximaNormal;

        // === SISTEMA DE BLOQUEIO REALISTA ===

        // Se está bloqueado à frente, NÃO permite movimento frontal
        if (bloqueadoFrente && inputVerticalSuavizado > 0)
        {
            inputVerticalSuavizado = 0;
            velocidadeDesejada = 0;

            if (inputVertical > 0.5f)
            {
                velocidadeAtual = Mathf.Max(velocidadeAtual - 0.5f * Time.deltaTime, -0.05f);
                Debug.Log("⚠️ Bloqueado à frente - impossível avançar!");
            }
        }
        // Se está bloqueado atrás, NÃO permite marcha-atrás
        else if (bloqueadoTras && inputVerticalSuavizado < 0)
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

        if (!bloqueadoFrente && !bloqueadoTras && Mathf.Abs(velocidadeDesejada) > Mathf.Abs(velocidadeAtual))
        {
            float aceleracao = velocidadeMaxima / tempoAceleracao;
            velocidadeAtual = Mathf.MoveTowards(velocidadeAtual, velocidadeDesejada, aceleracao * Time.deltaTime);
        }
        else
        {
            float desaceleracao = velocidadeMaxima / tempoTravagem;

            if (bloqueadoFrente || bloqueadoTras)
            {
                velocidadeAtual = 0;
            }
            else if (emColisao)
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
                    Debug.Log("⚠️ Direção Traseira: Use W/S + A/D para virar (como um carro)");
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

        if (deslizaParede && direcaoDeslize != Vector3.zero)
        {
            direcaoMovimento = direcaoDeslize * Mathf.Abs(velocidadeAtual) * 0.5f;
        }
        else
        {
            direcaoMovimento = transform.forward * velocidadeAtual;
        }

        direcaoMovimento.y = movimentoVelocidade.y;

        // === VERIFICAÇÃO PRÉVIA DE COLISÃO MELHORADA ===
        if (velocidadeAtual != 0)
        {
            // Usar distância muito pequena para verificação
            Vector3 proximaPosicao = transform.position + direcaoMovimento.normalized * 0.02f;
            if (!PodeMoverPara(proximaPosicao))
            {
                if (velocidadeAtual > 0) bloqueadoFrente = true;
                if (velocidadeAtual < 0) bloqueadoTras = true;
                tempoBloqueio = duracaoBloqueio;
                velocidadeAtual = 0;
                return;
            }
        }

        controller.Move(direcaoMovimento * Time.deltaTime);
    }

    void AplicarMovimentoVertical()
    {
        Vector3 movimentoVertical = new Vector3(0, movimentoVelocidade.y, 0);
        controller.Move(movimentoVertical * Time.deltaTime);
    }

    bool PodeMoverPara(Vector3 posicao)
    {
        Vector3 origem = transform.position + Vector3.up * 0.5f;
        Vector3 direcao = (posicao - transform.position).normalized;
        float distancia = Vector3.Distance(transform.position, posicao);

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

    void VerificarObstaculosCompleto()
    {
        // === ORIGEM DOS RAIOS AJUSTADA ===
        // Partir da borda do controller, não do centro!
        Vector3 centroController = transform.position + Vector3.up * 0.7f;
        
        avisoProximidade = false;
        bloqueadoFrente = false;
        bloqueadoTras = false;
        float menorDist = 999f;
        string objetoMaisProximo = "";

        // === VERIFICAÇÃO FRONTAL MELHORADA ===
        // Múltiplos raios começando NA BORDA do controller
        for (float offsetX = -0.2f; offsetX <= 0.2f; offsetX += 0.1f)
        {
            for (float offsetY = 0.2f; offsetY <= 1.0f; offsetY += 0.4f)
            {
                // Ponto de origem NA BORDA do cilindro do controller
                Vector3 origemLocal = centroController + transform.right * offsetX;
                origemLocal.y = transform.position.y + offsetY;
                
                // Adicionar o radius para começar na borda
                Vector3 origemRaio = origemLocal + transform.forward * controller.radius;
                
                RaycastHit hit;

                // Raio frontal - distância ajustada
                if (Physics.Raycast(origemRaio, transform.forward, out hit, distanciaAviso))
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

                        Vector3 projecao = Vector3.Project(transform.forward, hit.normal);
                        direcaoDeslize = (transform.forward - projecao).normalized;
                        deslizaParede = true;
                    }

                    if (dist < menorDist)
                    {
                        menorDist = dist;
                        objetoMaisProximo = hit.collider.name;
                        avisoProximidade = true;
                    }

                    // Debug visual melhorado
                    if (mostrarDebugRaios)
                    {
                        Color corRaio = dist < distanciaBloqueio ? Color.red : 
                                       (dist < 0.3f ? Color.yellow : Color.green);
                        Debug.DrawRay(origemRaio, transform.forward * hit.distance, corRaio);
                    }
                }
                else if (mostrarDebugRaios)
                {
                    // Mostrar raio mesmo sem hit
                    Debug.DrawRay(origemRaio, transform.forward * 0.5f, Color.gray);
                }

                // === VERIFICAÇÃO TRASEIRA ===
                Vector3 origemRaioTras = origemLocal - transform.forward * controller.radius;
                
                if (Physics.Raycast(origemRaioTras, -transform.forward, out hit, distanciaAviso * 0.5f))
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
                        Debug.DrawRay(origemRaioTras, -transform.forward * hit.distance, corRaio);
                    }
                }
            }
        }

        // === VERIFICAÇÃO LATERAL (360°) ===
        for (float angulo = -90f; angulo <= 90f; angulo += 30f)
        {
            if (angulo == 0) continue;

            Vector3 dir = Quaternion.Euler(0, angulo, 0) * transform.forward;
            Vector3 origemLateral = centroController + dir * controller.radius;
            
            RaycastHit hit;

            if (Physics.Raycast(origemLateral, dir, out hit, distanciaAviso * 0.7f))
            {
                string nomeObjeto = hit.collider.name.ToLower();
                if (nomeObjeto.Contains("plane") || nomeObjeto.Contains("ground") || nomeObjeto.Contains("floor"))
                    continue;

                if (hit.distance < menorDist)
                {
                    menorDist = hit.distance;
                    avisoProximidade = true;
                }

                if (mostrarDebugRaios)
                {
                    Debug.DrawRay(origemLateral, dir * hit.distance, Color.cyan);
                }
            }
        }

        distanciaObstaculo = menorDist;
        if (avisoProximidade && !emColisao)
        {
            objetoColidido = objetoMaisProximo;
        }

        // === DESENHAR O CILINDRO DO CONTROLLER (Debug Visual) ===
        if (mostrarDebugRaios)
        {
            DesenharCilindroController();
        }
    }

    // NOVO: Desenhar o cilindro do CharacterController na Scene View
    void DesenharCilindroController()
    {
        Vector3 centro = transform.position + controller.center;
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

    void AplicarGravidade()
    {
        if (controller.isGrounded)
        {
            movimentoVelocidade.y = -2f;
        }
        else
        {
            movimentoVelocidade.y += gravidade * Time.deltaTime;
        }
    }

    void PararDeEmergencia()
    {
        velocidadeAtual = 0;
        velocidadeDesejada = 0;
        deslizaParede = false;

        if (wheelController != null)
        {
            wheelController.PararRodas();
        }
    }

    void OnControllerColliderHit(ControllerColliderHit hit)
    {
        // Ignorar chão
        string nome = hit.gameObject.name.ToLower();
        if (nome.Contains("plane") || nome.Contains("ground") || nome.Contains("floor"))
            return;

        // Evitar múltiplas deteções
        if (Time.time - ultimoTempoColisao < 0.1f) return;

        // Determinar direção da colisão
        Vector3 dirParaObstaculo = (hit.point - transform.position);
        dirParaObstaculo.y = 0;
        dirParaObstaculo.Normalize();

        float angulo = Vector3.Angle(transform.forward, dirParaObstaculo);

        // BLOQUEIO baseado no ângulo
        if (angulo < 60f)
        {
            bloqueadoFrente = true;
            velocidadeAtual = 0;
            velocidadeDesejada = 0;

            Vector3 recuo = -transform.forward * 0.003f;
            recuo.y = 0;
            controller.Move(recuo);

            Debug.Log($"💥 COLISÃO FRONTAL com {hit.gameObject.name}");
        }
        else if (angulo > 120f)
        {
            bloqueadoTras = true;
            velocidadeAtual = 0;

            Vector3 empurrao = transform.forward * 0.003f;
            empurrao.y = 0;
            controller.Move(empurrao);

            Debug.Log($"💥 COLISÃO TRASEIRA com {hit.gameObject.name}");
        }
        else
        {
            normalColisao = hit.normal;
            Vector3 projecao = Vector3.Project(transform.forward, normalColisao);
            direcaoDeslize = (transform.forward - projecao).normalized;
            deslizaParede = true;

            Debug.Log($"💥 COLISÃO LATERAL com {hit.gameObject.name}");
        }

        emColisao = true;
        objetoColidido = hit.gameObject.name;
        pontoColisao = hit.point;
        tempoColisao = Time.time;
        ultimoTempoColisao = Time.time;
        tempoBloqueio = duracaoBloqueio;

        StartCoroutine(EfeitoColisao());
    }

    IEnumerator EfeitoColisao()
    {
        Vector3 posOriginal = transform.position;
        float duracao = 0.2f;
        float tempo = 0;

        while (tempo < duracao)
        {
            float intensidade = (1 - tempo / duracao) * 0.002f;
            transform.position = posOriginal + Random.insideUnitSphere * intensidade;
            tempo += Time.deltaTime;
            yield return null;
        }

        transform.position = posOriginal;
    }

    // Métodos públicos
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

    // GUI de debug
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

        // Estado
        string estado = "Normal";
        if (deslizaParede) estado = "Deslizar";
        else if (emColisao || bloqueadoFrente || bloqueadoTras) estado = "Colisão";

        // Amarelo para deslizar, vermelho para colisão, verde para normal
        if (deslizaParede) GUI.color = Color.yellow;
        else if (emColisao || bloqueadoFrente || bloqueadoTras) GUI.color = Color.red;
        else GUI.color = Color.green;

        GUI.Label(new Rect(15, 185, 240, 20), $"Estado: {estado}");
        GUI.color = Color.white;

        // Travão de emergência 
        if (travaoDeEmergencia)
        {
            GUI.color = new Color(1, 0, 0, 0.9f);
            GUI.Box(new Rect(10, 220, 250, 35), "");
            GUI.color = Color.red;  // Texto em vermelho
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