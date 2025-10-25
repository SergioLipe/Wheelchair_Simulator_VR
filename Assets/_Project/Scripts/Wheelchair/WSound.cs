using UnityEngine;
using System.Collections; // Precisamos disto para as Corrotinas

[RequireComponent(typeof(Movement))]
[RequireComponent(typeof(AudioSource))] 
public class WSound : MonoBehaviour
{
    [Header("Referências dos Audio Sources")]
    [Tooltip("O AudioSource que tem o som de ARRANQUE (toca 1 vez)")]
    public AudioSource audioArranque;

    [Tooltip("O AudioSource que tem o som de MOVIMENTO (em loop)")]
    public AudioSource audioMovimento;

    [Header("Configuração do Fade")]
    [Tooltip("Tempo (em segundos) que o som de movimento leva a desaparecer")]
    public float tempoFadeOut = 0.2f; // 0.2 segundos é muito rápido

    [Header("Referências")]
    private Movement movementController;

    // Estado
    private bool somArranqueTocou = false;
    private bool estaAcelerandoCache = false; // Guarda o estado anterior do INPUT
    
    // Controlo do Fade
    private Coroutine fadeOutCoroutine;
    private float volumeOriginalMovimento;

    void Start()
    {
        movementController = GetComponent<Movement>();

        if (audioArranque == null || audioMovimento == null)
        {
            Debug.LogError("Os Audio Sources (Arranque, Movimento) não foram definidos no WSound!");
            return; 
        }
        
        // Guarda o volume original para saber a quanto voltar
        volumeOriginalMovimento = audioMovimento.volume;
    }

    void Update()
    {
        // Lemos a variável pública do Movement.cs que regista o INPUT
        bool estaAcelerandoAgora = movementController.jogadorEstaAcelerando;

        // CASO 1: O jogador COMEÇOU a acelerar
        if (estaAcelerandoAgora && !estaAcelerandoCache)
        {
            TocarSonsInicio();
        }
        // CASO 2: O jogador PAROU de acelerar (largou a tecla)
        else if (!estaAcelerandoAgora && estaAcelerandoCache)
        {
            PararSons(); // (Esta função agora faz fade out)
        }

        // Atualiza o estado "cache" para o próximo frame
        estaAcelerandoCache = estaAcelerandoAgora;

        // --- Lógica de transição Arranque -> Loop ---
        if (somArranqueTocou && !audioArranque.isPlaying)
        {
            // Se o arranque acabou E o jogador AINDA está a acelerar
            if (estaAcelerandoAgora && !audioMovimento.isPlaying)
            {
                // Garante que o volume está no máximo antes de tocar o loop
                audioMovimento.volume = volumeOriginalMovimento; 
                audioMovimento.Play();
            }
            somArranqueTocou = false; 
        }
    }

    private void TocarSonsInicio()
    {
        // Se a cadeira estava a fazer fade out, cancela-o IMEDIATAMENTE!
        if (fadeOutCoroutine != null)
        {
            StopCoroutine(fadeOutCoroutine);
            fadeOutCoroutine = null;
        }

        // --- CORREÇÃO IMPORTANTE ---
        // Para garantir que o som de loop (que podia estar em fade out)
        // para IMEDIATAMENTE antes de o arranque começar.
        audioMovimento.Stop(); 
        // --- FIM DA CORREÇÃO ---

        // Restaura o volume do som de movimento (caso tenha sido
        // alterado pelo fade out)
        audioMovimento.volume = volumeOriginalMovimento; 

        // Para o som de arranque (para o caso de estar a tocar) e recomeça-o
        audioArranque.Stop();
        audioArranque.Play();
        somArranqueTocou = true;
    }

    private void PararSons()
    {
        // O arranque para sempre imediatamente
        audioArranque.Stop();
        somArranqueTocou = false;

        // Só fazemos fade out se o som de movimento ESTIVER a tocar
        if (audioMovimento.isPlaying)
        {
            // Inicia a corrotina de fade out
            fadeOutCoroutine = StartCoroutine(FadeOut(audioMovimento, tempoFadeOut));
        }
    }

    /// <summary>
    /// Corrotina que baixa o volume de um AudioSource até zero e depois pára-o.
    /// </summary>
    private IEnumerator FadeOut(AudioSource audioSource, float tempoDeFade)
    {
        float volumeInicial = audioSource.volume;
        float tempoPassado = 0f;

        while (tempoPassado < tempoDeFade)
        {
            // Calcula o novo volume
            tempoPassado += Time.deltaTime;
            audioSource.volume = Mathf.Lerp(volumeInicial, 0f, tempoPassado / tempoDeFade);
            
            // Espera até ao próximo frame
            yield return null; 
        }

        // Garante que fica em zero e pára o som
        audioSource.volume = 0f;
        audioSource.Stop();
        
        // Restaura o volume original para a próxima vez que tocar
        audioSource.volume = volumeOriginalMovimento; 
        fadeOutCoroutine = null; // Limpa a referência da corrotina
    }
}