using UnityEngine;

public class FreeLookCamera : MonoBehaviour
{
    [Header("=== Configurações do Olhar ===")]
    [Tooltip("Sensibilidade do rato")]
    public float sensibilidadeMouse = 2f;
    
    [Tooltip("Limite de olhar para CIMA (graus)")] // <<< MODIFICADO
    public float limiteVerticalCima = 80f;
    
    [Tooltip("Limite de olhar para BAIXO (quando olha em FRENTE) (graus)")] // <<< NOVO
    public float limiteVerticalBaixoFrontal = 80f;

    [Tooltip("Limite de olhar para BAIXO (quando olha para TRÁS) (graus)")] // <<< NOVO
    public float limiteVerticalBaixoTraseiro = 20f; // Ex: Um valor mais baixo para não ver o pescoço

    [Tooltip("Limite de olhar para esquerda/direita (graus)")]
    public float limiteHorizontal = 90f;  
    
    [Header("=== Suavização ===")]
    [Tooltip("Suavizar movimento da câmara")]
    public bool suavizarMovimento = true;
    
    [Tooltip("Velocidade de suavização")]
    public float velocidadeSuavizacao = 10f;
    
    [Header("=== Debug ===")]
    [SerializeField] private float rotacaoX = 0f;  // Cima/Baixo
    [SerializeField] private float rotacaoY = 0f;  // Esquerda/Direita
    
    // Variáveis internas
    private Quaternion rotacaoAlvo;
    
    void Start()
    {
        // Bloquear cursor no centro da tela
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;  //esconde o cursor
        
        // Guardar rotação inicial
        rotacaoAlvo = transform.localRotation;
    }
    
    void Update()
    {
        // Obter input do rato
        float mouseX = Input.GetAxis("Mouse X") * sensibilidadeMouse;
        float mouseY = Input.GetAxis("Mouse Y") * sensibilidadeMouse;
        
        // Aplicar rotações apenas se houver movimento do rato
        if (Mathf.Abs(mouseX) > 0.01f || Mathf.Abs(mouseY) > 0.01f)
        {
            // Rotação horizontal (virar cabeça esquerda/direita)
            rotacaoY += mouseX;
            rotacaoY = Mathf.Clamp(rotacaoY, -limiteHorizontal, limiteHorizontal);
            
            // Rotação vertical (olhar cima/baixo)
            rotacaoX -= mouseY;
            // A clamping (limitação) da rotação X é feita mais abaixo
        }

        // --- Lógica de Limite Vertical Dinâmico --- // <<< NOVO BLOCО
        
        // 1. Calcula o "fator" de quanto estamos a olhar para trás (0 = frente, 1 = totalmente atrás)
        // Usamos Mathf.Abs para tratar a esquerda e a direita da mesma forma.
        float fatorTras = Mathf.Abs(rotacaoY) / limiteHorizontal; 
        
        // 2. Interpola linearmente o limite de olhar para baixo
        // Lerp(a, b, t) -> se t=0, retorna 'a'. se t=1, retorna 'b'.
        float limiteBaixoAtual = Mathf.Lerp(
            limiteVerticalBaixoFrontal, 
            limiteVerticalBaixoTraseiro, 
            fatorTras
        );
        
        // 3. Aplica os limites verticais (clamping)
        rotacaoX = Mathf.Clamp(rotacaoX, -limiteVerticalCima, limiteBaixoAtual); // <<< MODIFICADO
        
        // ---------------------------------------------

        // Criar a rotação final
        rotacaoAlvo = Quaternion.Euler(rotacaoX, rotacaoY, 0f);
        
        // Aplicar rotação (com ou sem suavização)
        if (suavizarMovimento)
        {
            // Aplicar rotação suave usando interpolação esférica
            transform.localRotation = Quaternion.Slerp(
                transform.localRotation,
                rotacaoAlvo,
                velocidadeSuavizacao * Time.deltaTime
            );
        }
        else
        {
            // Aplicar rotação direta sem suavização
            transform.localRotation = rotacaoAlvo;
        }
    }
}