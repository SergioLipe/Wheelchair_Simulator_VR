using UnityEngine;

/// <summary>
/// Sistema de sons ultra-simplificado para cadeira de rodas elétrica
/// Apenas arranque e loop baseado no input do utilizador
/// COLOCA ESTE SCRIPT NO GameObject "Wheelchair" 
/// </summary>
public class Sounds : MonoBehaviour
{
    [Header("Audio Sources")]
    [Tooltip("AudioSource para o som do motor (loop contínuo)")]
    public AudioSource motorAudioSource;
    
    [Tooltip("AudioSource para sons pontuais (arranque, cliques, colisões)")]
    public AudioSource effectsAudioSource;

    [Header("Sons do Motor")]
    [Tooltip("Som de arranque (2 segundos)")]
    public AudioClip arranque;
    
    [Tooltip("Som contínuo do motor (loop)")]
    public AudioClip loop;

    [Header("Som de Interface")]
    [Tooltip("Som de clique ao mudar modos ou direção")]
    public AudioClip cliqueSound;

    [Header("Sons de Colisão")]
    [Tooltip("Som de colisão frontal/traseira")]
    public AudioClip colisaoFrontal;
    
    [Tooltip("Som de colisão lateral (deslizar)")]
    public AudioClip colisaoLateral;
    
    [Tooltip("Velocidade mínima de colisão para tocar som")]
    public float minCollisionVelocity = 0.5f;
    
    [Tooltip("Volume das colisões")]
    [Range(0f, 1f)]
    public float volumeColisao = 0.7f;

    [Header("Configurações do Motor")]
    [Tooltip("Volume do som de arranque")]
    [Range(0f, 1f)]
    public float volumeArranque = 0.7f;
    
    [Tooltip("Volume base do motor em loop")]
    [Range(0f, 1f)]
    public float volumeLoop = 0.5f;
    
    [Tooltip("Velocidade do fade out (segundos)")]
    [Range(0.5f, 5f)]
    public float fadeOutSpeed = 1f;
    
    [Header("Debug")]
    [SerializeField] private bool isAccelerating = false;
    [SerializeField] private bool arranqueIniciado = false;
    [SerializeField] private bool loopIniciado = false;
    [SerializeField] private float tempoAcelerando = 0f;

    void Start()
    {
        // Configurar AudioSource do motor
        if (motorAudioSource == null)
        {
            motorAudioSource = gameObject.AddComponent<AudioSource>();
        }
        motorAudioSource.loop = true;
        motorAudioSource.volume = 0f;
        motorAudioSource.playOnAwake = false;
        
        // Configurar AudioSource dos efeitos
        if (effectsAudioSource == null)
        {
            effectsAudioSource = gameObject.AddComponent<AudioSource>();
        }
        effectsAudioSource.loop = false;
        effectsAudioSource.playOnAwake = false;
        
        Debug.Log("✅ Sistema de sons inicializado!");
    }

    void Update()
    {
        // Verificar se o utilizador está a acelerar (W ou Seta para cima ou S ou Seta para baixo)
        float inputVertical = Input.GetAxis("Vertical");
        bool estaAcelerarAgora = Mathf.Abs(inputVertical) > 0.1f;
        
        // Se começou a acelerar
        if (estaAcelerarAgora && !isAccelerating)
        {
            IniciarAceleracao();
        }
        // Se parou de acelerar
        else if (!estaAcelerarAgora && isAccelerating)
        {
            PararAceleracao();
        }
        
        // Se está a acelerar, contar tempo
        if (isAccelerating)
        {
            tempoAcelerando += Time.deltaTime;
            
            // Após 2 segundos, iniciar loop se ainda não iniciou
            if (tempoAcelerando >= 2f && !loopIniciado)
            {
                IniciarLoop();
            }
        }
        
        // Fazer fade out quando não está a acelerar
        if (!isAccelerating && motorAudioSource.volume > 0.01f)
        {
            motorAudioSource.volume = Mathf.Lerp(motorAudioSource.volume, 0f, Time.deltaTime / fadeOutSpeed);
            
            if (motorAudioSource.volume < 0.01f)
            {
                motorAudioSource.Stop();
                motorAudioSource.volume = 0f;
            }
        }
    }

    /// <summary>
    /// Inicia a aceleração - toca som de arranque
    /// </summary>
    void IniciarAceleracao()
    {
        isAccelerating = true;
        tempoAcelerando = 0f;
        arranqueIniciado = true;
        loopIniciado = false;
        
        // Tocar som de arranque
        if (arranque != null && effectsAudioSource != null)
        {
            effectsAudioSource.PlayOneShot(arranque, volumeArranque);
        }
        
        Debug.Log("🚀 Arranque iniciado!");
    }

    /// <summary>
    /// Para a aceleração - inicia fade out
    /// </summary>
    void PararAceleracao()
    {
        isAccelerating = false;
        arranqueIniciado = false;
        loopIniciado = false;
        tempoAcelerando = 0f;
        
        Debug.Log("🛑 A fazer fade out...");
    }

    /// <summary>
    /// Inicia o loop do motor após 2 segundos
    /// </summary>
    void IniciarLoop()
    {
        if (loop != null && motorAudioSource != null)
        {
            loopIniciado = true;
            motorAudioSource.clip = loop;
            motorAudioSource.volume = volumeLoop;
            motorAudioSource.Play();
            
            Debug.Log("🔄 Loop iniciado!");
        }
    }

    /// <summary>
    /// Método PÚBLICO - Chamado pelo Movement quando começa/para movimento
    /// Mantido para compatibilidade mas não faz nada (o Update gere tudo)
    /// </summary>
    public void IniciarMovimento(bool modoInterior)
    {
        // Não precisa fazer nada - o Update gere tudo baseado no input
    }

    /// <summary>
    /// Método PÚBLICO - Chamado pelo Movement quando para
    /// Mantido para compatibilidade mas não faz nada (o Update gere tudo)
    /// </summary>
    public void PararMovimento()
    {
        // Não precisa fazer nada - o Update gere tudo baseado no input
    }

    /// <summary>
    /// Método PÚBLICO - Toca o som de clique
    /// </summary>
    public void TocarClique()
    {
        if (cliqueSound != null && effectsAudioSource != null)
        {
            effectsAudioSource.PlayOneShot(cliqueSound, 0.5f);
        }
    }

    /// <summary>
    /// Detecta colisões e toca sons apropriados
    /// </summary>
    void OnCollisionEnter(Collision collision)
    {
        float impactVelocity = collision.relativeVelocity.magnitude;

        if (impactVelocity >= minCollisionVelocity && effectsAudioSource != null)
        {
            // Determinar tipo de colisão baseado no ângulo
            Vector3 contactNormal = collision.GetContact(0).normal;
            float angulo = Vector3.Angle(transform.forward, -contactNormal);
            
            AudioClip somColisao = null;
            
            // Colisão frontal ou traseira
            if (angulo < 45f || angulo > 135f)
            {
                somColisao = colisaoFrontal;
            }
            // Colisão lateral
            else
            {
                somColisao = colisaoLateral;
            }
            
            if (somColisao != null)
            {
                effectsAudioSource.PlayOneShot(somColisao, volumeColisao);
            }
        }
    }
}