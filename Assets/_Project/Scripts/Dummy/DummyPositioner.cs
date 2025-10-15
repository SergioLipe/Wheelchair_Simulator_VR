using UnityEngine;

public class DummyPositioner : MonoBehaviour
{
    [Header("=== Ajustes do Corpo ===")]
    [Range(-45, 45)]
    public float inclinacaoCostas = -20f;
    
    [Range(0, 120)]
    public float dobraJoelhos = 90f;
    
    [Range(-30, 30)]
    public float abrirPernas = 10f;
    
    [Header("=== ▼ LADO ESQUERDO ▼ ===")]
    [Space(5)]
    [Header("Braço Esquerdo")]
    [Range(-180, 180)]
    [Tooltip("Abrir/fechar braço (negativo = fechar)")]
    public float abrirBracoEsquerdo = 0f;
    
    [Range(-90, 90)]
    [Tooltip("Levantar/baixar braço")]
    public float levantarBracoEsquerdo = 30f;
    
    [Range(-90, 90)]
    [Tooltip("Esticar braço para a frente/trás")]
    public float esticarBracoEsquerdo = 0f;
    
    [Range(0, 160)]
    [Tooltip("Dobrar cotovelo")]
    public float dobrarCotoveloEsquerdo = 45f;

    [Header("Pulso Esquerdo")]
    [Range(-90, 90)]
    [Tooltip("Dobrar para frente/trás")]
    public float dobrarPulsoEsquerdo = 0f;
    
    [Range(-90, 90)]
    [Tooltip("Rodar palma (pronação/supinação)")]
    public float rodarPulsoEsquerdo = 0f;
    
    [Range(-180, 180)]
    [Tooltip("Inclinar lateral (desvio radial/ulnar)")]
    public float inclinarPulsoEsquerdo = -45f;

    [Header("Dedos Esquerdos")]
    [Range(0, 100)]
    [Tooltip("0 = mão aberta, 100 = punho fechado")]
    public float fecharDedosEsquerdos = 0f;
    
    [Range(0, 100)]
    [Tooltip("Separar dedos (leque)")]
    public float separarDedosEsquerdos = 0f;
    
    [Space(5)]
    [Tooltip("Desativar animação do polegar (manter pose original)")]
    public bool desativarPolegarEsquerdo = true;
    
    [Header("Ajuste Manual Polegar Esquerdo")]
    [Range(-2f, 2f)]
    [Tooltip("Multiplicador do eixo X do polegar")]
    public float polegarEsquerdoMultX = 0f;
    
    [Range(-2f, 2f)]
    [Tooltip("Multiplicador do eixo Y do polegar")]
    public float polegarEsquerdoMultY = 0f;
    
    [Range(-2f, 2f)]
    [Tooltip("Multiplicador do eixo Z do polegar")]
    public float polegarEsquerdoMultZ = 0f;

    [Header("=== ▼ LADO DIREITO ▼ ===")]
    [Space(5)]
    [Header("Braço Direito")]
    [Range(-180, 180)]
    [Tooltip("Abrir/fechar braço (positivo = fechar)")]
    public float abrirBracoDireito = 0f;
    
    [Range(-90, 90)]
    [Tooltip("Levantar/baixar braço")]
    public float levantarBracoDireito = 30f;
    
    [Range(-90, 90)]
    [Tooltip("Esticar braço para a frente/trás")]
    public float esticarBracoDireito = 0f;
    
    [Range(0, 160)]
    [Tooltip("Dobrar cotovelo")]
    public float dobrarCotoveloDireito = 45f;

    [Header("Pulso Direito")]
    [Range(-90, 90)]
    [Tooltip("Dobrar para frente/trás")]
    public float dobrarPulsoDireito = 0f;
    
    [Range(-90, 90)]
    [Tooltip("Rodar palma (pronação/supinação)")]
    public float rodarPulsoDireito = 0f;
    
    [Range(-180, 180)]
    [Tooltip("Inclinar lateral (desvio radial/ulnar)")]
    public float inclinarPulsoDireito = -45f;

    [Header("Dedos Direitos")]
    [Range(0, 100)]
    [Tooltip("0 = mão aberta, 100 = punho fechado")]
    public float fecharDedosDireitos = 0f;
    
    [Range(0, 100)]
    [Tooltip("Separar dedos (leque)")]
    public float separarDedosDireitos = 0f;
    
    [Space(5)]
    [Tooltip("Desativar animação do polegar (manter pose original)")]
    public bool desativarPolegarDireito = true;
    
    [Header("Ajuste Manual Polegar Direito")]
    [Range(-2f, 2f)]
    [Tooltip("Multiplicador do eixo X do polegar")]
    public float polegarDireitoMultX = 0f;
    
    [Range(-2f, 2f)]
    [Tooltip("Multiplicador do eixo Y do polegar")]
    public float polegarDireitoMultY = 0f;
    
    [Range(-2f, 2f)]
    [Tooltip("Multiplicador do eixo Z do polegar")]
    public float polegarDireitoMultZ = 0f;

    [Header("=== Controlo Rápido ===")]
    [Tooltip("Aplicar mesmos valores aos dois braços")]
    public bool sincronizarBracos = false;
    
    [Tooltip("Aplicar mesmos valores aos dois pulsos")]
    public bool sincronizarPulsos = false;
    
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
    
    // Dedos da mão esquerda
    private Transform[] leftThumb = new Transform[4];
    private Transform[] leftIndex = new Transform[4];
    private Transform[] leftMiddle = new Transform[4];
    private Transform[] leftRing = new Transform[4];
    private Transform[] leftPinky = new Transform[4];
    
    // Dedos da mão direita
    private Transform[] rightThumb = new Transform[4];
    private Transform[] rightIndex = new Transform[4];
    private Transform[] rightMiddle = new Transform[4];
    private Transform[] rightRing = new Transform[4];
    private Transform[] rightPinky = new Transform[4];
    
    void Start()
    {
        EncontrarBones();
        AplicarPose();
    }
    
    void EncontrarBones()
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

            // === Pulsos ===  
            else if (nome == "mixamorig:LeftHand") leftHand = t;
            else if (nome == "mixamorig:RightHand") rightHand = t;
            
            // === DEDOS MÃO ESQUERDA ===
            // Polegar
            else if (nome == "mixamorig:LeftHandThumb1") leftThumb[0] = t;
            else if (nome == "mixamorig:LeftHandThumb2") leftThumb[1] = t;
            else if (nome == "mixamorig:LeftHandThumb3") leftThumb[2] = t;
            else if (nome == "mixamorig:LeftHandThumb4") leftThumb[3] = t;
            
            // Indicador
            else if (nome == "mixamorig:LeftHandIndex1") leftIndex[0] = t;
            else if (nome == "mixamorig:LeftHandIndex2") leftIndex[1] = t;
            else if (nome == "mixamorig:LeftHandIndex3") leftIndex[2] = t;
            else if (nome == "mixamorig:LeftHandIndex4") leftIndex[3] = t;
            
            // Médio
            else if (nome == "mixamorig:LeftHandMiddle1") leftMiddle[0] = t;
            else if (nome == "mixamorig:LeftHandMiddle2") leftMiddle[1] = t;
            else if (nome == "mixamorig:LeftHandMiddle3") leftMiddle[2] = t;
            else if (nome == "mixamorig:LeftHandMiddle4") leftMiddle[3] = t;
            
            // Anelar
            else if (nome == "mixamorig:LeftHandRing1") leftRing[0] = t;
            else if (nome == "mixamorig:LeftHandRing2") leftRing[1] = t;
            else if (nome == "mixamorig:LeftHandRing3") leftRing[2] = t;
            else if (nome == "mixamorig:LeftHandRing4") leftRing[3] = t;
            
            // Mindinho
            else if (nome == "mixamorig:LeftHandPinky1") leftPinky[0] = t;
            else if (nome == "mixamorig:LeftHandPinky2") leftPinky[1] = t;
            else if (nome == "mixamorig:LeftHandPinky3") leftPinky[2] = t;
            else if (nome == "mixamorig:LeftHandPinky4") leftPinky[3] = t;
            
            // === DEDOS MÃO DIREITA ===
            // Polegar
            else if (nome == "mixamorig:RightHandThumb1") rightThumb[0] = t;
            else if (nome == "mixamorig:RightHandThumb2") rightThumb[1] = t;
            else if (nome == "mixamorig:RightHandThumb3") rightThumb[2] = t;
            else if (nome == "mixamorig:RightHandThumb4") rightThumb[3] = t;
            
            // Indicador
            else if (nome == "mixamorig:RightHandIndex1") rightIndex[0] = t;
            else if (nome == "mixamorig:RightHandIndex2") rightIndex[1] = t;
            else if (nome == "mixamorig:RightHandIndex3") rightIndex[2] = t;
            else if (nome == "mixamorig:RightHandIndex4") rightIndex[3] = t;
            
            // Médio
            else if (nome == "mixamorig:RightHandMiddle1") rightMiddle[0] = t;
            else if (nome == "mixamorig:RightHandMiddle2") rightMiddle[1] = t;
            else if (nome == "mixamorig:RightHandMiddle3") rightMiddle[2] = t;
            else if (nome == "mixamorig:RightHandMiddle4") rightMiddle[3] = t;
            
            // Anelar
            else if (nome == "mixamorig:RightHandRing1") rightRing[0] = t;
            else if (nome == "mixamorig:RightHandRing2") rightRing[1] = t;
            else if (nome == "mixamorig:RightHandRing3") rightRing[2] = t;
            else if (nome == "mixamorig:RightHandRing4") rightRing[3] = t;
            
            // Mindinho
            else if (nome == "mixamorig:RightHandPinky1") rightPinky[0] = t;
            else if (nome == "mixamorig:RightHandPinky2") rightPinky[1] = t;
            else if (nome == "mixamorig:RightHandPinky3") rightPinky[2] = t;
            else if (nome == "mixamorig:RightHandPinky4") rightPinky[3] = t;
        }
    }
    
    void AplicarPose()
    {
        // === Inclinação das costas ===  
        if (spine != null) spine.localRotation = Quaternion.Euler(inclinacaoCostas, 0, 0);
        if (spine1 != null) spine1.localRotation = Quaternion.Euler(inclinacaoCostas * 0.5f, 0, 0);
        if (spine2 != null) spine2.localRotation = Quaternion.Euler(inclinacaoCostas * 0.3f, 0, 0);
        
        // === Pernas ===
        if (leftUpLeg != null)
            leftUpLeg.localRotation = Quaternion.Euler(-90, 180 - abrirPernas, 0);
        
        if (leftLeg != null)
            leftLeg.localRotation = Quaternion.Euler(-dobraJoelhos, 0, 0);
        
        if (rightUpLeg != null)
            rightUpLeg.localRotation = Quaternion.Euler(-90, 180 + abrirPernas, 0);
        
        if (rightLeg != null)
            rightLeg.localRotation = Quaternion.Euler(-dobraJoelhos, 0, 0);
        
        // === BRAÇO ESQUERDO ===
        if (leftArm != null)
        {
            // Rotações combinadas: levantar (X), abrir/fechar (Y), esticar frente (Z)
            leftArm.localRotation = Quaternion.Euler(
                levantarBracoEsquerdo,      // X: levantar/baixar
                -abrirBracoEsquerdo,        // Y: abrir/fechar (negativo fecha)
                esticarBracoEsquerdo        // Z: esticar para frente
            );
        }
        
        if (leftForeArm != null)
        {
            leftForeArm.localRotation = Quaternion.Euler(dobrarCotoveloEsquerdo, 0, 0);
        }
        
        // === BRAÇO DIREITO ===
        if (rightArm != null)
        {
            // Rotações combinadas: levantar (X), abrir/fechar (Y), esticar frente (Z)
            rightArm.localRotation = Quaternion.Euler(
                levantarBracoDireito,       // X: levantar/baixar
                abrirBracoDireito,          // Y: abrir/fechar (positivo fecha)
                -esticarBracoDireito        // Z: esticar para frente (invertido)
            );
        }
        
        if (rightForeArm != null)
        {
            rightForeArm.localRotation = Quaternion.Euler(dobrarCotoveloDireito, 0, 0);
        }
        
        // === PULSO ESQUERDO ===
        if (leftHand != null)
        {
            leftHand.localRotation = Quaternion.Euler(
                dobrarPulsoEsquerdo,      // X: flexão/extensão
                inclinarPulsoEsquerdo,    // Y: desvio radial/ulnar
                rodarPulsoEsquerdo        // Z: pronação/supinação
            );
        }
        
        // === PULSO DIREITO ===
        if (rightHand != null)
        {
            rightHand.localRotation = Quaternion.Euler(
                dobrarPulsoDireito,        // X: flexão/extensão
                -inclinarPulsoDireito,     // Y: desvio radial/ulnar (invertido)
                -rodarPulsoDireito         // Z: pronação/supinação (invertido)
            );
        }
        
        // === DEDOS ===
        AplicarDedos(leftThumb, leftIndex, leftMiddle, leftRing, leftPinky, 
                     fecharDedosEsquerdos, separarDedosEsquerdos, 
                     polegarEsquerdoMultX, polegarEsquerdoMultY, polegarEsquerdoMultZ, 
                     desativarPolegarEsquerdo, true);
        
        AplicarDedos(rightThumb, rightIndex, rightMiddle, rightRing, rightPinky, 
                     fecharDedosDireitos, separarDedosDireitos,
                     polegarDireitoMultX, polegarDireitoMultY, polegarDireitoMultZ,
                     desativarPolegarDireito, false);
    }
    
    // Método para aplicar rotação aos dedos
    void AplicarDedos(Transform[] polegar, Transform[] indicador, Transform[] medio, 
                      Transform[] anelar, Transform[] mindinho, 
                      float fechar, float separar, float polegarMultX, float polegarMultY, float polegarMultZ,
                      bool desativarPolegar, bool isLeft)
    {
        // Se fechar = 0 e separar = 0, resetar para pose original (mão aberta natural)
        if (fechar == 0f && separar == 0f)
        {
            // Reset todos os dedos para Quaternion.identity (pose original)
            for (int i = 0; i < 4; i++)
            {
                if (polegar[i] != null) polegar[i].localRotation = Quaternion.identity;
                if (indicador[i] != null) indicador[i].localRotation = Quaternion.identity;
                if (medio[i] != null) medio[i].localRotation = Quaternion.identity;
                if (anelar[i] != null) anelar[i].localRotation = Quaternion.identity;
                if (mindinho[i] != null) mindinho[i].localRotation = Quaternion.identity;
            }
            return;
        }
        
        // Converter de 0-100 para ângulos de rotação
        float fechamento = fechar * 1.3f; // Max ~130 graus
        float separacao = separar * 0.4f; // Max ~40 graus de spread
        
        // Sinal de direção (esquerda vs direita) para separação
        float dir = isLeft ? 1f : -1f;
        
        // === POLEGAR (com controlo manual dos eixos) ===
        // Se desativar polegar estiver ligado, manter pose original
        if (desativarPolegar)
        {
            // Manter polegar na pose original
            for (int i = 0; i < 4; i++)
            {
                if (polegar[i] != null) polegar[i].localRotation = Quaternion.identity;
            }
        }
        else
        {
            // Aplicar rotação manual ao polegar
            // Bone 0 - base do polegar (combina fechamento + separação)
            if (polegar[0] != null)
            {
                float x0 = fechamento * 0.5f * polegarMultX;
                float y0 = fechamento * 0.5f * polegarMultY;
                float z0 = fechamento * 0.5f * polegarMultZ + dir * separacao * 1.5f;
                polegar[0].localRotation = Quaternion.Euler(x0, y0, z0);
            }
            
            // Bones 1, 2, 3 - articulações do polegar (só fechamento)
            if (polegar[1] != null)
            {
                float x1 = fechamento * 0.7f * polegarMultX;
                float y1 = fechamento * 0.7f * polegarMultY;
                float z1 = fechamento * 0.7f * polegarMultZ;
                polegar[1].localRotation = Quaternion.Euler(x1, y1, z1);
            }
            
            if (polegar[2] != null)
            {
                float x2 = fechamento * 0.9f * polegarMultX;
                float y2 = fechamento * 0.9f * polegarMultY;
                float z2 = fechamento * 0.9f * polegarMultZ;
                polegar[2].localRotation = Quaternion.Euler(x2, y2, z2);
            }
            
            if (polegar[3] != null)
            {
                float x3 = fechamento * 1.0f * polegarMultX;
                float y3 = fechamento * 1.0f * polegarMultY;
                float z3 = fechamento * 1.0f * polegarMultZ;
                polegar[3].localRotation = Quaternion.Euler(x3, y3, z3);
            }
        }
        
        // === INDICADOR ===
        if (indicador[0] != null) 
            indicador[0].localRotation = Quaternion.Euler(fechamento * 0.8f, 0, dir * (-separacao * 0.5f));
        if (indicador[1] != null) 
            indicador[1].localRotation = Quaternion.Euler(fechamento * 0.9f, 0, 0);
        if (indicador[2] != null) 
            indicador[2].localRotation = Quaternion.Euler(fechamento * 1.0f, 0, 0);
        if (indicador[3] != null) 
            indicador[3].localRotation = Quaternion.Euler(fechamento * 1.1f, 0, 0);
        
        // === MÉDIO ===
        if (medio[0] != null) 
            medio[0].localRotation = Quaternion.Euler(fechamento * 0.8f, 0, dir * (-separacao * 0.2f));
        if (medio[1] != null) 
            medio[1].localRotation = Quaternion.Euler(fechamento * 0.9f, 0, 0);
        if (medio[2] != null) 
            medio[2].localRotation = Quaternion.Euler(fechamento * 1.0f, 0, 0);
        if (medio[3] != null) 
            medio[3].localRotation = Quaternion.Euler(fechamento * 1.1f, 0, 0);
        
        // === ANELAR ===
        if (anelar[0] != null) 
            anelar[0].localRotation = Quaternion.Euler(fechamento * 0.8f, 0, dir * separacao * 0.2f);
        if (anelar[1] != null) 
            anelar[1].localRotation = Quaternion.Euler(fechamento * 0.9f, 0, 0);
        if (anelar[2] != null) 
            anelar[2].localRotation = Quaternion.Euler(fechamento * 1.0f, 0, 0);
        if (anelar[3] != null) 
            anelar[3].localRotation = Quaternion.Euler(fechamento * 1.1f, 0, 0);
        
        // === MINDINHO ===
        if (mindinho[0] != null) 
            mindinho[0].localRotation = Quaternion.Euler(fechamento * 0.8f, 0, dir * separacao * 0.5f);
        if (mindinho[1] != null) 
            mindinho[1].localRotation = Quaternion.Euler(fechamento * 0.9f, 0, 0);
        if (mindinho[2] != null) 
            mindinho[2].localRotation = Quaternion.Euler(fechamento * 1.0f, 0, 0);
        if (mindinho[3] != null) 
            mindinho[3].localRotation = Quaternion.Euler(fechamento * 1.1f, 0, 0);
    }
    
    void OnValidate()
    {
        // Sincronização automática se ativada
        if (sincronizarBracos)
        {
            abrirBracoDireito = abrirBracoEsquerdo;
            levantarBracoDireito = levantarBracoEsquerdo;
            esticarBracoDireito = esticarBracoEsquerdo;
            dobrarCotoveloDireito = dobrarCotoveloEsquerdo;
        }
        
        if (sincronizarPulsos)
        {
            dobrarPulsoDireito = dobrarPulsoEsquerdo;
            rodarPulsoDireito = rodarPulsoEsquerdo;
            inclinarPulsoDireito = inclinarPulsoEsquerdo;
            fecharDedosDireitos = fecharDedosEsquerdos;
            separarDedosDireitos = separarDedosEsquerdos;
        }
        
        // Aplicar pose
        if (spine == null || leftArm == null) EncontrarBones();
        AplicarPose();
    }
    
    // === MÉTODOS ÚTEIS PARA PRESETS ===
    
    [ContextMenu("Preset: Mãos no Volante")]
    public void PresetMaosNoVolante()
    {
        // Braços à frente segurando volante
        levantarBracoEsquerdo = 20f;
        levantarBracoDireito = 20f;
        abrirBracoEsquerdo = 30f;
        abrirBracoDireito = 30f;
        esticarBracoEsquerdo = 40f;
        esticarBracoDireito = 40f;
        dobrarCotoveloEsquerdo = 60f;
        dobrarCotoveloDireito = 60f;
        
        // Pulsos ligeiramente dobrados
        dobrarPulsoEsquerdo = -10f;
        dobrarPulsoDireito = -10f;
        rodarPulsoEsquerdo = 0f;
        rodarPulsoDireito = 0f;
        
        // Dedos a segurar volante (semi-fechados)
        fecharDedosEsquerdos = 50f;
        fecharDedosDireitos = 50f;
        separarDedosEsquerdos = 10f;
        separarDedosDireitos = 10f;
        
        AplicarPose();
    }
    
    [ContextMenu("Preset: Mãos no Colo")]
    public void PresetMaosNoColo()
    {
        // Braços relaxados
        levantarBracoEsquerdo = -10f;
        levantarBracoDireito = -10f;
        abrirBracoEsquerdo = 20f;
        abrirBracoDireito = 20f;
        esticarBracoEsquerdo = 0f;
        esticarBracoDireito = 0f;
        dobrarCotoveloEsquerdo = 90f;
        dobrarCotoveloDireito = 90f;
        
        // Mãos relaxadas
        dobrarPulsoEsquerdo = 10f;
        dobrarPulsoDireito = 10f;
        rodarPulsoEsquerdo = 0f;
        rodarPulsoDireito = 0f;
        
        // Dedos relaxados (semi-abertos)
        fecharDedosEsquerdos = 20f;
        fecharDedosDireitos = 20f;
        separarDedosEsquerdos = 5f;
        separarDedosDireitos = 5f;
        
        AplicarPose();
    }
    
    [ContextMenu("Preset: Braço Esquerdo no Joystick")]
    public void PresetBracoEsquerdoJoystick()
    {
        // Braço esquerdo no joystick da cadeira
        levantarBracoEsquerdo = 0f;
        abrirBracoEsquerdo = -10f;
        esticarBracoEsquerdo = 10f;
        dobrarCotoveloEsquerdo = 90f;
        dobrarPulsoEsquerdo = -20f;
        rodarPulsoEsquerdo = -15f;
        
        // Mão esquerda a segurar joystick
        fecharDedosEsquerdos = 60f;
        separarDedosEsquerdos = 5f;
        
        // Braço direito relaxado
        levantarBracoDireito = -10f;
        abrirBracoDireito = 20f;
        esticarBracoDireito = 0f;
        dobrarCotoveloDireito = 90f;
        
        // Mão direita relaxada
        fecharDedosDireitos = 20f;
        separarDedosDireitos = 5f;
        
        AplicarPose();
    }
    
    [ContextMenu("Preset: Punhos Fechados")]
    public void PresetPunhosFechados()
    {
        // Fechar ambas as mãos completamente
        fecharDedosEsquerdos = 100f;
        fecharDedosDireitos = 100f;
        separarDedosEsquerdos = 0f;
        separarDedosDireitos = 0f;
        
        AplicarPose();
    }
    
    [ContextMenu("Preset: Mãos Abertas")]
    public void PresetMaosAbertas()
    {
        // Abrir ambas as mãos completamente
        fecharDedosEsquerdos = 0f;
        fecharDedosDireitos = 0f;
        separarDedosEsquerdos = 100f;
        separarDedosDireitos = 100f;
        
        AplicarPose();
    }
    
    [ContextMenu("Reset Pose")]
    public void ResetPose()
    {
        inclinacaoCostas = -20f;
        dobraJoelhos = 90f;
        abrirPernas = 10f;
        
        abrirBracoEsquerdo = 0f;
        levantarBracoEsquerdo = 30f;
        esticarBracoEsquerdo = 0f;
        dobrarCotoveloEsquerdo = 45f;
        
        abrirBracoDireito = 0f;
        levantarBracoDireito = 30f;
        esticarBracoDireito = 0f;
        dobrarCotoveloDireito = 45f;
        
        dobrarPulsoEsquerdo = 0f;
        rodarPulsoEsquerdo = 0f;
        inclinarPulsoEsquerdo = -45f;
        
        dobrarPulsoDireito = 0f;
        rodarPulsoDireito = 0f;
        inclinarPulsoDireito = -45f;
        
        fecharDedosEsquerdos = 0f;
        separarDedosEsquerdos = 0f;
        fecharDedosDireitos = 0f;
        separarDedosDireitos = 0f;
        
        AplicarPose();
    }
}