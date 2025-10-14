using UnityEngine;
using System.Collections;
using System.Collections.Generic;

/*
 * Sistema de Movimento Realista para Cadeira de Rodas Elétrica
 * 
 * Características:
 * - Direção Frontal: Rodas frontais direcionais (pode rodar parado se configurado)
 * - Direção Traseira: Rodas traseiras direcionais (comportamento tipo carro - só vira em movimento)
 * - Sistema de colisão realista que bloqueia movimento ao bater
 * - Controlo total do utilizador sobre velocidade e travagem
 * - Marcha-atrás com rotação invertida em direção traseira
 */

public class WheelchairMovement : MonoBehaviour
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
    
    [Tooltip("Força de resistência ao bater (não usado - controlo total do utilizador)")]
    [Range(0f, 1f)]
    public float resistenciaColisao = 0.8f;

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
    [SerializeField] private float eficienciaRotacao = 100f; // Percentagem de rotação baseada na velocidade

    // Componentes
    private CharacterController controller;
    private Vector3 movimentoVelocidade;
    private WheelchairWheelController wheelController;

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
    private const float duracaoBloqueio = 0.2f; // Bloqueia por 0.2 segundos após colisão (mais curto)
    
    // Sistema de deslizamento em paredes
    private bool deslizandoParede = false;
    private Vector3 direcaoDeslize = Vector3.zero;
    
    // Direção traseira - feedback
    private bool tentandoVirarParado = false;
    private float tempoTentandoVirar = 0f;

    public enum ModosVelocidade
    {
        Lento,      // Para interiores
        Normal,     // Uso geral
        Desligado   // Travão de emergência
    }

    void Start()
    {
        // Configurar o CharacterController com valores MÍNIMOS
        controller = GetComponent<CharacterController>();
        if (controller == null)
        {
            controller = gameObject.AddComponent<CharacterController>();
        }
        
        // Valores ajustados para scale 1x1x1
        controller.height = 1.4f;
        controller.radius = 0.35f;
        controller.center = new Vector3(0, 0.7f, 0);
        
        // CRÍTICO: Skin Width deve ser maior que zero mas pequeno
        controller.skinWidth = 0.01f;  // Valor menor para permitir chegar mais perto
        controller.minMoveDistance = 0.001f;
        controller.stepOffset = 0.1f;
        
        // Elevar um pouco no início para não ficar preso no chão
        transform.position += Vector3.up * 0.1f;

        // Obter referência ao wheel controller
        wheelController = GetComponent<WheelchairWheelController>();

        // Converter km/h para m/s
        velocidadeMaximaNormal = velocidadeMaximaNormal / 3.6f;
        velocidadeMaximaLenta = velocidadeMaximaLenta / 3.6f;
        velocidadeMarchaAtras = velocidadeMarchaAtras / 3.6f;
        
        Debug.Log("✅ WheelchairMovement - Sistema de Colisão Realista ativo!");
        Debug.Log("📌 Direção Traseira = comportamento tipo carro (só roda em movimento)");
        Debug.Log("📌 Direção Frontal = pode rodar parado se configurado");
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
        }

        // Aplicar sempre a gravidade
        AplicarGravidade();
        
        // Reset automático da colisão após 0.5 segundos
        if (emColisao && Time.time - tempoColisao > 0.5f)
        {
            emColisao = false;
            objetoColidido = "";
            deslizandoParede = false;
        }
    }

    void GerirModos()
    {
        // Tecla 1: Modo Lento (interior/pessoas)
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
        float inputVertical = Input.GetAxis("Vertical");    // W/S ou Setas
        float inputHorizontal = Input.GetAxis("Horizontal"); // A/D ou Setas

        // Suavizar o input (simula o joystick analógico da cadeira)
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
            inputVerticalSuavizado = 0; // Bloqueia completamente movimento frontal
            velocidadeDesejada = 0;
            
            // Só mostra feedback se jogador insiste em ir para frente
            if (inputVertical > 0.5f) // Se jogador insiste em ir para frente
            {
                // Adicionar pequeno recuo para mostrar resistência
                velocidadeAtual = Mathf.Max(velocidadeAtual - 0.5f * Time.deltaTime, -0.05f);  // Recuo mais sutil
                Debug.Log("⚠️ Bloqueado à frente - impossível avançar!");
            }
        }
        // Se está bloqueado atrás, NÃO permite marcha-atrás
        else if (bloqueadoTras && inputVerticalSuavizado < 0)
        {
            inputVerticalSuavizado = 0; // Bloqueia marcha-atrás
            velocidadeDesejada = 0;
        }
        // Movimento normal quando não bloqueado
        else
        {
            // Marcha-atrás é sempre mais lenta
            if (inputVerticalSuavizado < 0)
            {
                velocidadeMaxima = velocidadeMarchaAtras;
            }

            // Calcular velocidade desejada - CONTROLO TOTAL DO UTILIZADOR
            velocidadeDesejada = inputVerticalSuavizado * velocidadeMaxima;
            
            // NÃO reduzir velocidade automaticamente - o utilizador controla tudo
        }

        // === ACELERAÇÃO E DESACELERAÇÃO ===
        
        // Acelerar apenas se não está bloqueado
        if (!bloqueadoFrente && !bloqueadoTras && Mathf.Abs(velocidadeDesejada) > Mathf.Abs(velocidadeAtual))
        {
            float aceleracao = velocidadeMaxima / tempoAceleracao;
            velocidadeAtual = Mathf.MoveTowards(velocidadeAtual, velocidadeDesejada, aceleracao * Time.deltaTime);
        }
        else
        {
            // Desacelerar/Travar
            float desaceleracao = velocidadeMaxima / tempoTravagem;
            
            // Travagem imediata se está bloqueado
            if (bloqueadoFrente || bloqueadoTras)
            {
                velocidadeAtual = 0; // Para instantaneamente se bloqueado
            }
            else if (emColisao)
            {
                desaceleracao *= 2f; // Trava 2x mais rápido em colisão
                velocidadeAtual = Mathf.MoveTowards(velocidadeAtual, velocidadeDesejada, desaceleracao * Time.deltaTime);
            }
            else
            {
                // Desaceleração normal quando o utilizador solta os controlos
                velocidadeAtual = Mathf.MoveTowards(velocidadeAtual, velocidadeDesejada, desaceleracao * Time.deltaTime);
            }
        }

        // Rotação - sempre permitida mesmo quando bloqueado
        ProcessarRotacao(inputHorizontalSuavizado);
    }

    void ProcessarRotacao(float inputHorizontal)
    {
        float multiplicadorRotacao = 1f;
        bool isDirecaoTraseira = false;
        eficienciaRotacao = 100f; // Reset

        // Se o wheelController existir, verificar tipo de direção
        if (wheelController != null)
        {
            isDirecaoTraseira = wheelController.GetTipoDirecao() == WheelchairWheelController.TipoDirecao.DirecaoTraseira;
            
            if (isDirecaoTraseira)
            {
                multiplicadorRotacao = 1.3f;  // 30% mais ágil quando em movimento
            }
        }

        bool estaParado = Mathf.Abs(velocidadeAtual) < 0.1f;

        // === COMPORTAMENTO REALISTA DA DIREÇÃO TRASEIRA ===
        if (isDirecaoTraseira)
        {
            // Direção traseira: SÓ roda quando está em movimento!
            if (estaParado)
            {
                eficienciaRotacao = 0f; // Sem rotação quando parado
                
                // Feedback visual quando tenta virar parado
                if (Mathf.Abs(inputHorizontal) > 0.1f)
                {
                    tentandoVirarParado = true;
                    tempoTentandoVirar = 1f; // Mostra aviso por 1 segundo
                    
                    // Debug mais informativo
                    Debug.Log("⚠️ Direção Traseira: Use W/S + A/D para virar (como um carro)");
                }
                
                // NÃO permite rodar parado com direção traseira (realista)
                return; // NÃO roda a cadeira quando parado
            }
            else
            {
                tentandoVirarParado = false; // Limpa o aviso quando em movimento
                
                // Em movimento: rotação proporcional à velocidade (como um carro)
                float velocidadeNormalizada = Mathf.Abs(velocidadeAtual) / velocidadeMaximaNormal;
                
                // Quanto mais devagar, menos vira (realista)
                // Em velocidade máxima = 100% rotação
                // Em velocidade mínima = 20% rotação
                float eficienciaBase = Mathf.Lerp(0.2f, 1f, velocidadeNormalizada);
                multiplicadorRotacao *= eficienciaBase;
                
                // Marcha-atrás: direção invertida (como um carro real)
                // Quando vai para trás e vira à direita, a frente vai para a esquerda
                if (velocidadeAtual < 0)
                {
                    multiplicadorRotacao *= -0.8f; // Inverte e reduz a 80% (marcha-atrás é mais lenta)
                    eficienciaRotacao = eficienciaBase * 80f; // 80% eficiência em marcha-atrás
                }
                else
                {
                    eficienciaRotacao = eficienciaBase * 100f;
                }
            }
        }
        // === COMPORTAMENTO DA DIREÇÃO FRONTAL ===
        else
        {
            tentandoVirarParado = false; // Não aplica para direção frontal
            
            // Direção frontal: comportamento original
            if (estaParado && !rotacaoNoLugar)
            {
                eficienciaRotacao = 0f;
                return; // Não roda se está parado e não pode rodar no lugar
            }
            else if (estaParado && rotacaoNoLugar)
            {
                multiplicadorRotacao *= 1.5f; // Rotação mais rápida quando parado (frontal pode fazer isto)
                eficienciaRotacao = 100f; // 100% eficiência quando pode rodar parado
            }
            else
            {
                // Em movimento: rotação normal
                float velocidadeNormalizada = Mathf.Abs(velocidadeAtual) / velocidadeMaximaNormal;
                multiplicadorRotacao *= (1f + velocidadeNormalizada * 0.2f);
                eficienciaRotacao = 100f; // Direção frontal sempre 100% eficiente
            }
        }

        // Aplicar rotação
        float rotacao = inputHorizontal * velocidadeRotacao * multiplicadorRotacao * Time.deltaTime;
        transform.Rotate(0, rotacao, 0);
    }

    void AplicarMovimentoRealista()
    {
        Vector3 direcaoMovimento = Vector3.zero;
        
        // Se está deslizando numa parede, usar direção de deslize
        if (deslizandoParede && direcaoDeslize != Vector3.zero)
        {
            direcaoMovimento = direcaoDeslize * Mathf.Abs(velocidadeAtual) * 0.5f; // Desliza a 50% da velocidade
        }
        else
        {
            // Movimento normal
            direcaoMovimento = transform.forward * velocidadeAtual;
        }
        
        // Aplicar gravidade
        direcaoMovimento.y = movimentoVelocidade.y;
        
        // === VERIFICAÇÃO PRÉVIA DE COLISÃO ===
        // Verifica se vai colidir ANTES de mover
        if (velocidadeAtual != 0)
        {
            Vector3 proximaPosicao = transform.position + direcaoMovimento.normalized * 0.05f; // Reduzido de 0.1f
            if (!PodeMoverPara(proximaPosicao))
            {
                // Bloqueia movimento se vai colidir
                if (velocidadeAtual > 0) bloqueadoFrente = true;
                if (velocidadeAtual < 0) bloqueadoTras = true;
                tempoBloqueio = duracaoBloqueio;
                velocidadeAtual = 0;
                return;
            }
        }
        
        // Aplicar movimento
        controller.Move(direcaoMovimento * Time.deltaTime);
    }

    bool PodeMoverPara(Vector3 posicao)
    {
        // Verificar se a posição está livre usando raycasts
        Vector3 origem = transform.position + Vector3.up * 0.5f;
        Vector3 direcao = (posicao - transform.position).normalized;
        float distancia = Vector3.Distance(transform.position, posicao);
        
        RaycastHit hit;
        if (Physics.Raycast(origem, direcao, out hit, distancia + 0.05f)) // Reduzido de 0.2f para 0.05f
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
        Vector3 origem = transform.position + Vector3.up * 0.5f;
        avisoProximidade = false;
        bloqueadoFrente = false;
        bloqueadoTras = false;
        float menorDist = 999f;
        string objetoMaisProximo = "";
        
        // Verificar frente com múltiplos raios (mais preciso)
        for (float offsetX = -0.3f; offsetX <= 0.3f; offsetX += 0.15f)
        {
            Vector3 origemRaio = origem + transform.right * offsetX;
            RaycastHit hit;
            
            // Raio frontal - distância de verificação maior para avisos
            if (Physics.Raycast(origemRaio, transform.forward, out hit, distanciaAviso))
            {
                // Ignorar chão
                string nomeObjeto = hit.collider.name.ToLower();
                if (nomeObjeto.Contains("plane") || nomeObjeto.Contains("ground") || nomeObjeto.Contains("floor"))
                    continue;
                
                float dist = hit.distance;
                
                // BLOQUEIO FRONTAL só quando REALMENTE próximo (quase a tocar)
                if (dist < 0.12f)  // Ainda mais próximo - permite quase encostar
                {
                    bloqueadoFrente = true;
                    tempoBloqueio = duracaoBloqueio;
                    normalColisao = hit.normal;
                    
                    // Calcular direção de deslize ao longo da parede
                    Vector3 projecao = Vector3.Project(transform.forward, hit.normal);
                    direcaoDeslize = (transform.forward - projecao).normalized;
                    deslizandoParede = true;
                }
                
                if (dist < menorDist)
                {
                    menorDist = dist;
                    objetoMaisProximo = hit.collider.name;
                    avisoProximidade = true;
                }
                
                // Debug visual - cores ajustadas para novas distâncias
                Color corRaio = dist < 0.12f ? Color.red : (dist < 0.3f ? Color.yellow : Color.green);
                Debug.DrawRay(origemRaio, transform.forward * hit.distance, corRaio);
            }
            
            // Raio traseiro - distância menor
            if (Physics.Raycast(origemRaio, -transform.forward, out hit, distanciaAviso * 0.3f))  
            {
                string nomeObjeto = hit.collider.name.ToLower();
                if (nomeObjeto.Contains("plane") || nomeObjeto.Contains("ground") || nomeObjeto.Contains("floor"))
                    continue;
                
                if (hit.distance < 0.12f)  // Consistente com a frente
                {
                    bloqueadoTras = true;
                    tempoBloqueio = duracaoBloqueio;
                }
                
                Debug.DrawRay(origemRaio, -transform.forward * hit.distance, Color.magenta);
            }
        }
        
        // Verificar laterais (para avisos)
        for (float angulo = -90f; angulo <= 90f; angulo += 30f)
        {
            if (angulo == 0) continue; // Já verificado acima
            
            Vector3 dir = Quaternion.Euler(0, angulo, 0) * transform.forward;
            RaycastHit hit;
            
            if (Physics.Raycast(origem, dir, out hit, distanciaAviso * 0.7f))
            {
                string nomeObjeto = hit.collider.name.ToLower();
                if (nomeObjeto.Contains("plane") || nomeObjeto.Contains("ground") || nomeObjeto.Contains("floor"))
                    continue;
                
                if (hit.distance < menorDist)
                {
                    menorDist = hit.distance;
                    avisoProximidade = true;
                }
                
                Debug.DrawRay(origem, dir * hit.distance, Color.cyan);
            }
        }
        
        distanciaObstaculo = menorDist;
        if (avisoProximidade && !emColisao)
        {
            objetoColidido = objetoMaisProximo;
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
        // Parar imediatamente
        velocidadeAtual = 0;
        velocidadeDesejada = 0;
        
        // NÃO resetar bloqueios - mantém o estado atual de colisão
        // Só limpa o deslizamento
        deslizandoParede = false;
        
        // Parar as rodas
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
        
        // Evitar múltiplas deteções no mesmo frame
        if (Time.time - ultimoTempoColisao < 0.1f) return;
        
        // Determinar direção da colisão
        Vector3 dirParaObstaculo = (hit.point - transform.position);
        dirParaObstaculo.y = 0;
        dirParaObstaculo.Normalize();
        
        float angulo = Vector3.Angle(transform.forward, dirParaObstaculo);
        
        // BLOQUEIO IMEDIATO baseado no ângulo
        if (angulo < 60f) // Colisão frontal
        {
            bloqueadoFrente = true;
            velocidadeAtual = 0; // Para imediatamente
            velocidadeDesejada = 0;
            
            // Empurrar ligeiramente para trás (recuo realista mas sutil)
            Vector3 recuo = -transform.forward * 0.005f;  // Reduzido de 0.02f
            recuo.y = 0;
            controller.Move(recuo);
            
            Debug.Log($"💥 COLISÃO FRONTAL - Movimento bloqueado!");
        }
        else if (angulo > 120f) // Colisão traseira
        {
            bloqueadoTras = true;
            velocidadeAtual = 0;
            
            // Empurrar ligeiramente para frente
            Vector3 empurrao = transform.forward * 0.005f;  // Reduzido de 0.02f
            empurrao.y = 0;
            controller.Move(empurrao);
            
            Debug.Log($"💥 COLISÃO TRASEIRA - Marcha-atrás bloqueada!");
        }
        else // Colisão lateral - permite deslizar
        {
            // Calcular direção de deslize
            normalColisao = hit.normal;
            Vector3 projecao = Vector3.Project(transform.forward, normalColisao);
            direcaoDeslize = (transform.forward - projecao).normalized;
            deslizandoParede = true;
            
            Debug.Log($"💥 COLISÃO LATERAL - Deslizando pela parede");
        }
        
        // Registar colisão
        emColisao = true;
        objetoColidido = hit.gameObject.name;
        pontoColisao = hit.point;
        tempoColisao = Time.time;
        ultimoTempoColisao = Time.time;
        tempoBloqueio = duracaoBloqueio;
        
        // Vibração visual
        StartCoroutine(EfeitoColisao());
    }

    IEnumerator EfeitoColisao()
    {
        Vector3 posOriginal = transform.position;
        float duracao = 0.2f;
        float tempo = 0;
        
        while (tempo < duracao)
        {
            float intensidade = (1 - tempo / duracao) * 0.002f;  // Reduzido de 0.005f - mais sutil
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

    // GUI de debug melhorada
    void OnGUI()
    {
        if (!Application.isEditor) return;

        // Info de movimento
        GUI.color = new Color(0, 0, 0, 0.8f);
        GUI.Box(new Rect(10, 100, 250, 140), "");

        GUI.color = Color.white;
        GUI.Label(new Rect(15, 105, 240, 20), "=== CADEIRA DE RODAS ===");
        GUI.Label(new Rect(15, 125, 240, 20), $"Modo: {modoAtual}");
        GUI.Label(new Rect(15, 145, 240, 20), $"Velocidade: {(velocidadeAtual * 3.6f):F1} / {(modoAtual == ModosVelocidade.Lento ? 3 : 6)} km/h");
        GUI.Label(new Rect(15, 165, 240, 20), $"Direção: {tipoDirecaoAtual}");
        
        // Mostrar eficiência de rotação apenas em direção traseira
        if (tipoDirecaoAtual.Contains("Traseira") && Mathf.Abs(Input.GetAxis("Horizontal")) > 0.1f)
        {
            GUI.color = eficienciaRotacao < 30f ? Color.red : (eficienciaRotacao < 70f ? Color.yellow : Color.green);
            GUI.Label(new Rect(15, 185, 240, 20), $"Rotação: {eficienciaRotacao:F0}% (baseado na velocidade)");
            GUI.color = Color.white;
            GUI.Label(new Rect(15, 205, 240, 20), $"Distância Obstáculo: {(distanciaObstaculo < 10 ? $"{distanciaObstaculo:F2}m" : "Livre")}");
        }
        else
        {
            GUI.Label(new Rect(15, 185, 240, 20), $"Distância Obstáculo: {(distanciaObstaculo < 10 ? $"{distanciaObstaculo:F2}m" : "Livre")}");
        }
        // Só mostra estado se houver algo relevante
        if (emColisao || bloqueadoFrente || bloqueadoTras || deslizandoParede)
        {
            string estado = "Normal";
            if (bloqueadoFrente) estado = "BLOQUEADO FRENTE!";
            else if (bloqueadoTras) estado = "BLOQUEADO TRÁS!";
            else if (deslizandoParede) estado = "Deslizando";
            else if (emColisao) estado = "Colisão!";
            
            GUI.color = (bloqueadoFrente || bloqueadoTras) ? Color.red : (deslizandoParede ? Color.yellow : Color.white);
            GUI.Label(new Rect(15, 205, 240, 20), $"Estado: {estado}");
            GUI.color = Color.white;
        }
        
        if (objetoColidido != "")
        {
            GUI.Label(new Rect(15, 225, 240, 20), $"Objeto: {objetoColidido}");
        }

        if (travaoDeEmergencia)
        {
            GUI.color = Color.red;
            GUI.Label(new Rect(15, 225, 240, 20), "🛑 TRAVÃO ATIVO!");
        }

        // AVISO DE COLISÃO - Simplificado
        if (bloqueadoFrente || bloqueadoTras)
        {
            GUI.color = new Color(1, 0, 0, 0.9f);
            GUI.Box(new Rect(10, 250, 250, 60), "");
            GUI.color = Color.white;
            
            if (bloqueadoFrente)
            {
                GUI.Label(new Rect(15, 255, 240, 20), "❌ BLOQUEADO À FRENTE");
                GUI.Label(new Rect(15, 275, 240, 20), "Use S para recuar ou A/D para rodar");
            }
            else if (bloqueadoTras)
            {
                GUI.Label(new Rect(15, 255, 240, 20), "❌ BLOQUEADO ATRÁS");
                GUI.Label(new Rect(15, 275, 240, 20), "Use W para avançar ou A/D para rodar");
            }
        }
        // Aviso de direção traseira parada
        else if (tempoTentandoVirar > 0 && tipoDirecaoAtual.Contains("Traseira"))
        {
            GUI.color = new Color(1, 0.5f, 0, 0.8f);
            GUI.Box(new Rect(10, 250, 250, 60), "");
            GUI.color = Color.white;
            GUI.Label(new Rect(15, 255, 240, 20), "⚠ DIREÇÃO TRASEIRA");
            GUI.Label(new Rect(15, 270, 240, 20), "Não roda parado (como um carro)");
            GUI.Label(new Rect(15, 285, 240, 20), "Use W/S + A/D para virar");
        }
        // Aviso muito discreto de proximidade
        else if (avisoProximidade && distanciaObstaculo < 0.18f)  // Só quando quase a tocar
        {
            GUI.color = Color.yellow;
            GUI.Label(new Rect(15, 245, 240, 20), $"⚠ Obstáculo a {distanciaObstaculo:F1}m");
        }

        // Controlos - posição dinâmica baseada no que está visível
        int yPosControlos = 270;
        if (bloqueadoFrente || bloqueadoTras) yPosControlos = 320;
        else if (tempoTentandoVirar > 0) yPosControlos = 320;
        
        GUI.color = new Color(0, 0.5f, 0, 0.8f);
        GUI.Box(new Rect(10, yPosControlos, 250, 85), "");
        GUI.color = Color.white;
        GUI.Label(new Rect(15, yPosControlos + 5, 240, 20), "=== CONTROLOS ===");
        GUI.Label(new Rect(15, yPosControlos + 25, 240, 20), "WASD/Setas - Mover");
        GUI.Label(new Rect(15, yPosControlos + 40, 240, 20), "1/2 - Modo Lento/Normal");
        GUI.Label(new Rect(15, yPosControlos + 55, 240, 20), "T - Alternar direção");
        GUI.Label(new Rect(15, yPosControlos + 70, 240, 20), "ESPAÇO - Travão");
    }
}