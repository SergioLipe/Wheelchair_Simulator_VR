using UnityEngine;

/// <summary>
/// Sistema de efeito visual de flash quando há colisões
/// Mostra flash vermelho para colisões frontais/traseiras
/// Mostra flash amarelo para deslizamentos laterais
/// </summary>
public class CollisionFlashEffect : MonoBehaviour
{
    [Header("=== Configuração do Flash ===")]
    [Tooltip("Ativar efeito de flash na tela")]
    public bool flashAtivo = true;

    [Tooltip("Duração do flash (segundos)")]
    public float duracaoFlash = 0.3f;

    [Tooltip("Opacidade máxima do flash (0-1)")]
    [Range(0f, 1f)]
    public float opacidadeMaxima = 0.6f;

    // Tipos de flash
    public enum TipoFlash 
    { 
        Nenhum, 
        Frontal, 
        Traseiro, 
        LateralEsquerda, 
        LateralDireita 
    }

    // Estado atual do flash
    private TipoFlash flashAtual = TipoFlash.Nenhum;
    private float intensidadeFlash = 0f;
    private float tempoFlash = 0f;

    void Update()
    {
        // Atualizar flash visual
        if (tempoFlash > 0)
        {
            tempoFlash -= Time.deltaTime;
            intensidadeFlash = Mathf.Lerp(1f, 0f, 1f - (tempoFlash / duracaoFlash));
            
            if (tempoFlash <= 0)
            {
                flashAtual = TipoFlash.Nenhum;
                intensidadeFlash = 0f;
            }
        }
    }

    /// <summary>
    /// Ativar flash de colisão frontal (vermelho em cima)
    /// </summary>
    public void FlashFrontal()
    {
        AtivarFlash(TipoFlash.Frontal);
    }

    /// <summary>
    /// Ativar flash de colisão traseira (vermelho em baixo)
    /// </summary>
    public void FlashTraseiro()
    {
        AtivarFlash(TipoFlash.Traseiro);
    }

    /// <summary>
    /// Ativar flash lateral esquerdo (amarelo à esquerda)
    /// </summary>
    public void FlashLateralEsquerdo()
    {
        AtivarFlash(TipoFlash.LateralEsquerda);
    }

    /// <summary>
    /// Ativar flash lateral direito (amarelo à direita)
    /// </summary>
    public void FlashLateralDireito()
    {
        AtivarFlash(TipoFlash.LateralDireita);
    }

    /// <summary>
    /// Ativar o flash visual
    /// </summary>
    void AtivarFlash(TipoFlash tipo)
    {
        if (!flashAtivo) return;

        flashAtual = tipo;
        intensidadeFlash = 1f;
        tempoFlash = duracaoFlash;
    }

    /// <summary>
    /// Desenhar o flash de colisão na tela
    /// </summary>
    void OnGUI()
    {
        if (!flashAtivo || flashAtual == TipoFlash.Nenhum || intensidadeFlash <= 0)
            return;

        // Calcular opacidade do flash (fade out)
        float alpha = 1f - intensidadeFlash;
        alpha = Mathf.Clamp01(alpha * 1.5f); // Fade mais rápido

        // Cor base do flash
        Color corFlash;
        if (flashAtual == TipoFlash.Frontal || flashAtual == TipoFlash.Traseiro)
            corFlash = new Color(1f, 0f, 0f, alpha * opacidadeMaxima); // Vermelho
        else
            corFlash = new Color(1f, 1f, 0f, alpha * (opacidadeMaxima * 0.8f)); // Amarelo

        // Criar textura temporária
        Texture2D textura = new Texture2D(1, 1);
        textura.SetPixel(0, 0, corFlash);
        textura.Apply();

        // Desenhar o flash baseado no tipo
        switch (flashAtual)
        {
            case TipoFlash.Frontal:
                // Flash em cima (1/4 superior da tela)
                GUI.DrawTexture(new Rect(0, 0, Screen.width, Screen.height * 0.25f), textura);
                break;

            case TipoFlash.Traseiro:
                // Flash em baixo (1/4 inferior da tela)
                GUI.DrawTexture(new Rect(0, Screen.height * 0.75f, Screen.width, Screen.height * 0.25f), textura);
                break;

            case TipoFlash.LateralEsquerda:
                // Flash à esquerda (1/4 lateral esquerdo)
                GUI.DrawTexture(new Rect(0, 0, Screen.width * 0.25f, Screen.height), textura);
                break;

            case TipoFlash.LateralDireita:
                // Flash à direita (1/4 lateral direito)
                GUI.DrawTexture(new Rect(Screen.width * 0.75f, 0, Screen.width * 0.25f, Screen.height), textura);
                break;
        }

        // Limpar textura
        Object.Destroy(textura);
    }

    /// <summary>
    /// Parar qualquer flash ativo
    /// </summary>
    public void PararFlash()
    {
        flashAtual = TipoFlash.Nenhum;
        intensidadeFlash = 0f;
        tempoFlash = 0f;
    }
}