using UnityEngine;
using UnityEngine.UI;
using System.Collections;

/// <summary>
/// Sistema moderno de feedback visual de colisões
/// Usa UI Canvas com gradientes, animações suaves e efeitos direcionais
/// </summary>
public class CollisionFlashEffect : MonoBehaviour
{
    [Header("=== Configuração Geral ===")]
    [Tooltip("Ativar sistema de feedback")]
    public bool feedbackAtivo = true;

    [Tooltip("Referência ao Canvas (será criado automaticamente se null)")]
    public Canvas canvas;

    [Header("=== Configuração Visual ===")]
    [Tooltip("Duração do efeito (segundos)")]
    [Range(0.1f, 2f)]
    public float duracaoEfeito = 0.5f;

    [Tooltip("Intensidade máxima do efeito (0-1)")]
    [Range(0f, 1f)]
    public float intensidadeMaxima = 0.7f;

    [Tooltip("Usar gradiente radial (mais moderno)")]
    public bool usarGradienteRadial = true;

    [Header("=== Cores ===")]
    [Tooltip("Cor para colisões frontais/traseiras")]
    public Color corImpacto = new Color(1f, 0.2f, 0.2f, 1f); // Vermelho vibrante

    [Tooltip("Cor para deslizamentos laterais")]
    public Color corDeslizamento = new Color(1f, 0.8f, 0f, 1f); // Amarelo/laranja

    [Header("=== Animação ===")]
    [Tooltip("Curva de animação do efeito")]
    public AnimationCurve curvaAnimacao = AnimationCurve.EaseInOut(0, 1, 1, 0);

    [Tooltip("Usar efeito de pulso")]
    public bool efeitoPulso = true;

    [Tooltip("Número de pulsos")]
    [Range(1, 3)]
    public int numeroPulsos = 1;

    [Header("=== Camera Shake (Opcional) ===")]
    [Tooltip("Ativar tremor de câmera")]
    public bool cameraShakeAtivo = true;

    [Tooltip("Intensidade do tremor")]
    [Range(0f, 1f)]
    public float intensidadeTremor = 0.15f;

    [Tooltip("Duração do tremor")]
    [Range(0.05f, 0.5f)]
    public float duracaoTremor = 0.2f;

    [Header("=== Efeitos Extras ===")]
    [Tooltip("Mostrar setas direcionais")]
    public bool mostrarSetas = true;

    [Tooltip("Tamanho das setas")]
    [Range(50f, 200f)]
    public float tamanhoSeta = 100f;

    // Tipos de colisão
    public enum TipoColisao
    {
        Nenhum,
        Frontal,
        Traseiro,
        LateralEsquerda,
        LateralDireita
    }

    // Componentes UI
    private GameObject painelEfeito;
    private Image imagemEfeito;
    private GameObject[] setas = new GameObject[4]; // Frontal, Traseiro, Esquerda, Direita
    private Image[] imagensSetas = new Image[4];

    // Estado da animação
    private Coroutine corotinaAtual;
    private Transform cameraTransform;
    private Vector3 posicaoOriginalCamera;

    void Start()
    {
        ConfigurarUI();
        
        // Obter referência da câmera para shake
        if (Camera.main != null)
        {
            cameraTransform = Camera.main.transform;
            posicaoOriginalCamera = cameraTransform.localPosition;
        }
    }

    /// <summary>
    /// Configura o Canvas e elementos UI
    /// </summary>
    void ConfigurarUI()
    {
        // Criar Canvas se não existir
        if (canvas == null)
        {
            GameObject canvasObj = new GameObject("CollisionFeedbackCanvas");
            canvas = canvasObj.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 1000; // Garantir que fica em cima

            CanvasScaler scaler = canvasObj.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);

            canvasObj.AddComponent<GraphicRaycaster>();
        }

        // Criar painel principal de efeito
        painelEfeito = new GameObject("PainelEfeito");
        painelEfeito.transform.SetParent(canvas.transform, false);

        RectTransform rectEfeito = painelEfeito.AddComponent<RectTransform>();
        rectEfeito.anchorMin = Vector2.zero;
        rectEfeito.anchorMax = Vector2.one;
        rectEfeito.sizeDelta = Vector2.zero;

        imagemEfeito = painelEfeito.AddComponent<Image>();
        imagemEfeito.color = new Color(1, 1, 1, 0);
        imagemEfeito.raycastTarget = false;

        // Criar setas direcionais
        if (mostrarSetas)
        {
            CriarSetas();
        }
    }

    /// <summary>
    /// Cria as setas direcionais
    /// </summary>
    void CriarSetas()
    {
        string[] nomes = { "SetaFrontal", "SetaTraseiro", "SetaEsquerda", "SetaDireita" };
        Vector2[] posicoes = {
            new Vector2(0.5f, 0.85f),  // Cima
            new Vector2(0.5f, 0.15f),  // Baixo
            new Vector2(0.15f, 0.5f),  // Esquerda
            new Vector2(0.85f, 0.5f)   // Direita
        };
        float[] rotacoes = { 0f, 180f, 90f, -90f };

        for (int i = 0; i < 4; i++)
        {
            setas[i] = new GameObject(nomes[i]);
            setas[i].transform.SetParent(canvas.transform, false);

            RectTransform rect = setas[i].AddComponent<RectTransform>();
            rect.anchorMin = posicoes[i];
            rect.anchorMax = posicoes[i];
            rect.sizeDelta = new Vector2(tamanhoSeta, tamanhoSeta);
            rect.localRotation = Quaternion.Euler(0, 0, rotacoes[i]);

            imagensSetas[i] = setas[i].AddComponent<Image>();
            imagensSetas[i].color = new Color(1, 1, 1, 0);
            imagensSetas[i].raycastTarget = false;

            // Criar sprite de seta (triângulo simples)
            imagensSetas[i].sprite = CriarSpriteSeta();
        }
    }

    /// <summary>
    /// Cria um sprite de seta proceduralmente
    /// </summary>
    Sprite CriarSpriteSeta()
    {
        int tamanho = 128;
        Texture2D textura = new Texture2D(tamanho, tamanho, TextureFormat.RGBA32, false);
        Color[] pixels = new Color[tamanho * tamanho];

        // Preencher com transparente
        for (int i = 0; i < pixels.Length; i++)
            pixels[i] = Color.clear;

        // Desenhar triângulo apontando para cima
        for (int y = 0; y < tamanho; y++)
        {
            for (int x = 0; x < tamanho; x++)
            {
                float normY = (float)y / tamanho;
                float centroX = tamanho / 2f;
                float largura = (1f - normY) * tamanho / 2f;

                if (x >= centroX - largura && x <= centroX + largura)
                {
                    pixels[y * tamanho + x] = Color.white;
                }
            }
        }

        textura.SetPixels(pixels);
        textura.Apply();

        return Sprite.Create(textura, new Rect(0, 0, tamanho, tamanho), new Vector2(0.5f, 0.5f));
    }

    /// <summary>
    /// Ativar feedback de colisão frontal
    /// </summary>
    public void FlashFrontal()
    {
        AtivarFeedback(TipoColisao.Frontal);
    }

    /// <summary>
    /// Ativar feedback de colisão traseira
    /// </summary>
    public void FlashTraseiro()
    {
        AtivarFeedback(TipoColisao.Traseiro);
    }

    /// <summary>
    /// Ativar feedback de deslizamento lateral esquerdo
    /// </summary>
    public void FlashLateralEsquerdo()
    {
        AtivarFeedback(TipoColisao.LateralEsquerda);
    }

    /// <summary>
    /// Ativar feedback de deslizamento lateral direito
    /// </summary>
    public void FlashLateralDireito()
    {
        AtivarFeedback(TipoColisao.LateralDireita);
    }

    /// <summary>
    /// Ativa o feedback visual
    /// </summary>
    void AtivarFeedback(TipoColisao tipo)
    {
        if (!feedbackAtivo || tipo == TipoColisao.Nenhum) return;

        // Parar animação anterior se existir
        if (corotinaAtual != null)
            StopCoroutine(corotinaAtual);

        // Iniciar nova animação
        corotinaAtual = StartCoroutine(AnimarFeedback(tipo));

        // Camera shake
        if (cameraShakeAtivo && cameraTransform != null)
        {
            StartCoroutine(CameraShake());
        }
    }

    /// <summary>
    /// Corrotina principal de animação
    /// </summary>
    IEnumerator AnimarFeedback(TipoColisao tipo)
    {
        // Determinar cor e direção
        Color cor = (tipo == TipoColisao.Frontal || tipo == TipoColisao.Traseiro) 
            ? corImpacto : corDeslizamento;

        // Criar textura de gradiente
        Texture2D textura = CriarTexturaGradiente(tipo);
        Sprite sprite = Sprite.Create(textura, new Rect(0, 0, textura.width, textura.height), 
            new Vector2(0.5f, 0.5f));
        imagemEfeito.sprite = sprite;

        // Mostrar seta correspondente
        int indiceSeta = -1;
        if (mostrarSetas)
        {
            switch (tipo)
            {
                case TipoColisao.Frontal: indiceSeta = 0; break;
                case TipoColisao.Traseiro: indiceSeta = 1; break;
                case TipoColisao.LateralEsquerda: indiceSeta = 2; break;
                case TipoColisao.LateralDireita: indiceSeta = 3; break;
            }
        }

        // Animar
        float tempoDecorrido = 0f;
        float duracaoPorPulso = duracaoEfeito / numeroPulsos;

        for (int pulso = 0; pulso < numeroPulsos; pulso++)
        {
            float tempoInicioPulso = tempoDecorrido;

            while (tempoDecorrido - tempoInicioPulso < duracaoPorPulso)
            {
                tempoDecorrido += Time.deltaTime;
                float progresso = (tempoDecorrido - tempoInicioPulso) / duracaoPorPulso;
                float intensidade = curvaAnimacao.Evaluate(progresso) * intensidadeMaxima;

                // Aplicar cor com intensidade
                Color corAtual = cor;
                corAtual.a = intensidade;
                imagemEfeito.color = corAtual;

                // Animar seta
                if (indiceSeta >= 0)
                {
                    Color corSeta = cor;
                    corSeta.a = intensidade * 1.5f; // Setas mais visíveis
                    imagensSetas[indiceSeta].color = corSeta;

                    // Pulsar tamanho da seta
                    float escala = 1f + Mathf.Sin(progresso * Mathf.PI) * 0.3f;
                    setas[indiceSeta].transform.localScale = Vector3.one * escala;
                }

                yield return null;
            }
        }

        // Fade out final
        imagemEfeito.color = new Color(cor.r, cor.g, cor.b, 0);
        if (indiceSeta >= 0)
        {
            imagensSetas[indiceSeta].color = new Color(cor.r, cor.g, cor.b, 0);
            setas[indiceSeta].transform.localScale = Vector3.one;
        }

        // Limpar textura
        Destroy(textura);
        Destroy(sprite);

        corotinaAtual = null;
    }

    /// <summary>
    /// Cria textura de gradiente direcional
    /// </summary>
    Texture2D CriarTexturaGradiente(TipoColisao tipo)
    {
        int largura = 512;
        int altura = 512;
        Texture2D textura = new Texture2D(largura, altura, TextureFormat.RGBA32, false);

        for (int y = 0; y < altura; y++)
        {
            for (int x = 0; x < largura; x++)
            {
                float alpha = 0f;

                if (usarGradienteRadial)
                {
                    // Gradiente radial das bordas
                    float distX = Mathf.Abs(x - largura / 2f) / (largura / 2f);
                    float distY = Mathf.Abs(y - altura / 2f) / (altura / 2f);
                    
                    switch (tipo)
                    {
                        case TipoColisao.Frontal:
                            alpha = 1f - (distX * 0.7f + (1f - (float)y / altura) * 0.3f);
                            break;
                        case TipoColisao.Traseiro:
                            alpha = 1f - (distX * 0.7f + ((float)y / altura) * 0.3f);
                            break;
                        case TipoColisao.LateralEsquerda:
                            alpha = 1f - (distY * 0.7f + (1f - (float)x / largura) * 0.3f);
                            break;
                        case TipoColisao.LateralDireita:
                            alpha = 1f - (distY * 0.7f + ((float)x / largura) * 0.3f);
                            break;
                    }
                }
                else
                {
                    // Gradiente linear simples
                    switch (tipo)
                    {
                        case TipoColisao.Frontal:
                            alpha = 1f - (float)y / altura;
                            break;
                        case TipoColisao.Traseiro:
                            alpha = (float)y / altura;
                            break;
                        case TipoColisao.LateralEsquerda:
                            alpha = 1f - (float)x / largura;
                            break;
                        case TipoColisao.LateralDireita:
                            alpha = (float)x / largura;
                            break;
                    }
                }

                alpha = Mathf.Clamp01(alpha);
                textura.SetPixel(x, y, new Color(1, 1, 1, alpha));
            }
        }

        textura.Apply();
        return textura;
    }

    /// <summary>
    /// Efeito de tremor de câmera
    /// </summary>
    IEnumerator CameraShake()
    {
        float tempoDecorrido = 0f;

        while (tempoDecorrido < duracaoTremor)
        {
            float intensidade = Mathf.Lerp(intensidadeTremor, 0f, tempoDecorrido / duracaoTremor);
            
            Vector3 offset = new Vector3(
                Random.Range(-1f, 1f) * intensidade,
                Random.Range(-1f, 1f) * intensidade,
                0
            );

            cameraTransform.localPosition = posicaoOriginalCamera + offset;

            tempoDecorrido += Time.deltaTime;
            yield return null;
        }

        cameraTransform.localPosition = posicaoOriginalCamera;
    }

    /// <summary>
    /// Parar todos os efeitos
    /// </summary>
    public void PararTodosEfeitos()
    {
        if (corotinaAtual != null)
        {
            StopCoroutine(corotinaAtual);
            corotinaAtual = null;
        }

        if (imagemEfeito != null)
            imagemEfeito.color = new Color(1, 1, 1, 0);

        for (int i = 0; i < imagensSetas.Length; i++)
        {
            if (imagensSetas[i] != null)
            {
                imagensSetas[i].color = new Color(1, 1, 1, 0);
                if (setas[i] != null)
                    setas[i].transform.localScale = Vector3.one;
            }
        }

        if (cameraTransform != null)
            cameraTransform.localPosition = posicaoOriginalCamera;
    }

    void OnDestroy()
    {
        PararTodosEfeitos();
    }
}