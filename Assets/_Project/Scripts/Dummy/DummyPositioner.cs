using UnityEngine;

public class DummyPositioner : MonoBehaviour
{
    [Header("=== Ajustes Simples ===")]
    [Range(-45, 45)]
    public float inclinacaoCostas = -20f;
    
    [Range(0, 120)]
    public float dobraJoelhos = 90f;
    
    [Range(-30, 30)]
    public float abrirPernas = 10f;
    
    [Header("=== Ajustes dos Braços ===")]
    [Range(-180, 180)]
    public float abrirBracos = 0f; // negativo = fechar (palmas), positivo = abrir
    
    [Range(-90, 90)]
    public float levantarBracos = 30f;
    
    [Range(0, 160)]
    public float dobrarCotovelos = 45f;

    public enum EixoAbrir { X, Y, Z }
    public EixoAbrir abrirEixo = EixoAbrir.Y; // escolhe qual eixo abre/fecha

    [Header("=== Ajustes dos Pulsos ===")]
    [Range(-90, 90)]
    public float dobrarPulsos = 0f;   // frente/trás (X)
    
    [Range(-90, 90)]
    public float rodarPulsos = 0f;    // rodar palma (Z)
    
    // Bones
    private Transform hips;
    private Transform spine;
    private Transform spine1;
    private Transform spine2;
    private Transform leftUpLeg;
    private Transform leftLeg;
    private Transform rightUpLeg;
    private Transform rightLeg;
    private Transform leftArm;
    private Transform leftForeArm;
    private Transform rightArm;
    private Transform rightForeArm;
    private Transform leftHand;
    private Transform rightHand;
    
    void Start()
    {
        EncontrarBones();
        AplicarPose();
    }
    
    void EncontrarBones()      // Procurar os ossos
    {
        Transform[] todos = GetComponentsInChildren<Transform>();
        
        foreach (Transform t in todos)
        {
            string nome = t.name;
            
            // === Coluna === 
            if (nome.Contains("Hips")) hips = t;
            else if (nome == "mixamorig:Spine") spine = t;
            else if (nome == "mixamorig:Spine1") spine1 = t;
            else if (nome == "mixamorig:Spine2") spine2 = t;
            
            // === Pernas === 
            else if (nome == "mixamorig:LeftUpLeg") leftUpLeg = t;
            else if (nome == "mixamorig:LeftLeg") leftLeg = t;
            else if (nome == "mixamorig:RightUpLeg") rightUpLeg = t;
            else if (nome == "mixamorig:RightLeg") rightLeg = t;
            
            // === Braços === 
            else if (nome == "mixamorig:LeftArm") leftArm = t;
            else if (nome == "mixamorig:LeftForeArm") leftForeArm = t;
            else if (nome == "mixamorig:RightArm") rightArm = t;
            else if (nome == "mixamorig:RightForeArm") rightForeArm = t;

            // ===  Pulsos ===  
            else if (nome == "mixamorig:LeftHand") leftHand = t;
            else if (nome == "mixamorig:RightHand") rightHand = t;
        }
    }
    
    void AplicarPose()
    {
        // ===  Inclinação das costas ===  
        if (spine != null) spine.localRotation = Quaternion.Euler(inclinacaoCostas, 0, 0);
        if (spine1 != null) spine1.localRotation = Quaternion.Euler(inclinacaoCostas * 0.5f, 0, 0);
        if (spine2 != null) spine2.localRotation = Quaternion.Euler(inclinacaoCostas * 0.3f, 0, 0);
        
        // === Pernas  ===
        if (leftUpLeg != null)
            leftUpLeg.localRotation = Quaternion.Euler(-90, 180 - abrirPernas, 0);
        
        if (leftLeg != null)
            leftLeg.localRotation = Quaternion.Euler(-dobraJoelhos, 0, 0);
        
        if (rightUpLeg != null)
            rightUpLeg.localRotation = Quaternion.Euler(-90, 180 + abrirPernas, 0);
        
        if (rightLeg != null)
            rightLeg.localRotation = Quaternion.Euler(-dobraJoelhos, 0, 0);
        
        // === Braços ===
        Quaternion qLift = Quaternion.Euler(levantarBracos, 0, 0);

        Quaternion qOpenLeft, qOpenRight;
        switch (abrirEixo)
        {
            case EixoAbrir.X:
                qOpenLeft = Quaternion.Euler(-abrirBracos, 0, 0);
                qOpenRight = Quaternion.Euler(abrirBracos, 0, 0);
                break;
            case EixoAbrir.Z:
                qOpenLeft = Quaternion.Euler(0, 0, -abrirBracos);
                qOpenRight = Quaternion.Euler(0, 0, abrirBracos);
                break;
            default: // Y
                qOpenLeft = Quaternion.Euler(0, -abrirBracos, 0);
                qOpenRight = Quaternion.Euler(0, abrirBracos, 0);
                break;
        }

        if (leftArm != null)
            leftArm.localRotation = qLift * qOpenLeft;
        
        if (rightArm != null)
            rightArm.localRotation = qLift * qOpenRight;
        
        if (leftForeArm != null)
            leftForeArm.localRotation = Quaternion.Euler(dobrarCotovelos, 0, 0);
        
        if (rightForeArm != null)
            rightForeArm.localRotation = Quaternion.Euler(dobrarCotovelos, 0, 0);
        
        // === Pulsos ===
        if (leftHand != null)
            leftHand.localRotation = Quaternion.Euler(dobrarPulsos, 0, rodarPulsos);
        
        if (rightHand != null)
            rightHand.localRotation = Quaternion.Euler(dobrarPulsos, 0, -rodarPulsos);
    }
    
    void OnValidate()     // Quando algo muda
    {
        if (spine == null || leftArm == null) EncontrarBones();      // Verifica se já encontrou os ossos
        AplicarPose();
    }
}
