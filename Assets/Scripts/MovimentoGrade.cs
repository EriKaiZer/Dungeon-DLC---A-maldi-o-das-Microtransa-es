using UnityEngine;
using UnityEngine.InputSystem;

// Substitui o conteudo do MovimentoGrade.cs existente. Nao adicione uma segunda copia.
public class MovimentoGrade : MonoBehaviour
{
    [Header("Caminhada livre")]
    [Min(0.1f)] public float velocidade = 3.5f;
    public float gravidade = -20f;
    public bool bloquearControles;

    [Header("Camera com mouse")]
    [Range(0.01f, 0.5f)] public float sensibilidadeMouse = 0.09f;
    [Range(10f, 89f)] public float limiteVertical = 75f;

    [Header("Passos estilo Doom (somente visual)")]
    [Range(0f, 0.15f)] public float alturaBalanco = 0.055f;
    [Range(0f, 0.08f)] public float balancoLateral = 0.018f;
    [Min(0.1f)] public float distanciaEntrePassos = 0.8f;

    [Header("Impacto do combate")]
    [Range(0f, 2f)] public float forcaImpacto = 1f;

    private CharacterController controlador;
    private Transform cameraJogador;
    private Vector3 posicaoOriginalCamera;
    private Quaternion rotacaoOriginalCamera;
    private float inclinacao;
    private float queda;
    private float fasePasso;
    private Vector3 balancoAtual;
    private CursorLockMode lockAnterior;
    private bool visivelAnterior;
    private bool iniciou;
    private bool pularDelta;
    private float impactoRestante;
    private float ladoImpacto;
    private float intensidadeImpacto;

    void Start()
    {
        var cam = GetComponentInChildren<Camera>();
        if (cam == null || cam.transform == transform)
        {
            Debug.LogError("MovimentoGrade: a camera precisa estar dentro do Jogador, em um objeto filho.", this);
            enabled = false;
            return;
        }
        var corpo = GetComponent<Rigidbody>();
        if (corpo != null && !corpo.isKinematic)
        {
            Debug.LogError("MovimentoGrade: existe um Rigidbody dinamico no Jogador. Ative Is Kinematic nele antes de usar CharacterController.", this);
            enabled = false;
            return;
        }
        cameraJogador = cam.transform;
        posicaoOriginalCamera = cameraJogador.localPosition;
        rotacaoOriginalCamera = cameraJogador.localRotation;
        controlador = GetComponent<CharacterController>();
        if (controlador == null)
        {
            controlador = gameObject.AddComponent<CharacterController>();
            // Usa o chao na origem do jogador e a altura atual dos olhos como referencia.
            float alturaOlhos = transform.InverseTransformPoint(cameraJogador.position).y;
            controlador.height = Mathf.Max(1.8f, alturaOlhos + 0.2f);
            controlador.radius = 0.28f;
            controlador.center = Vector3.up * (controlador.height * 0.5f + 0.03f);
            controlador.stepOffset = 0.25f;
            controlador.skinWidth = 0.03f;
            controlador.minMoveDistance = 0f;
        }
        lockAnterior = Cursor.lockState;
        visivelAnterior = Cursor.visible;
        iniciou = true;
        CapturarMouse();
    }

    void Update()
    {
        if (!iniciou || !controlador.enabled) return;
        var teclado = Keyboard.current;
        var mouse = Mouse.current;
        bool escapar = teclado != null && teclado.escapeKey.wasPressedThisFrame;
        if (escapar || bloquearControles || Time.timeScale <= 0f || !Application.isFocused)
        {
            LiberarMouse();
            SuavizarBalanco(Vector3.zero);
            return;
        }
        if (Cursor.lockState != CursorLockMode.Locked)
        {
            if (mouse != null && mouse.leftButton.wasPressedThisFrame) CapturarMouse();
            return;
        }

        if (mouse != null && !pularDelta)
        {
            Vector2 delta = mouse.delta.ReadValue() * sensibilidadeMouse;
            transform.Rotate(0f, delta.x, 0f, Space.Self);
            inclinacao = Mathf.Clamp(inclinacao - delta.y, -limiteVertical, limiteVertical);
            cameraJogador.localRotation = rotacaoOriginalCamera * Quaternion.Euler(inclinacao, 0f, 0f);
        }
        pularDelta = false;

        Vector2 entrada = Vector2.zero;
        if (teclado != null)
        {
            entrada.x = (teclado.dKey.isPressed ? 1f : 0f) - (teclado.aKey.isPressed ? 1f : 0f);
            entrada.y = (teclado.wKey.isPressed ? 1f : 0f) - (teclado.sKey.isPressed ? 1f : 0f);
        }
        entrada = Vector2.ClampMagnitude(entrada, 1f);
        Vector3 frente = Vector3.ProjectOnPlane(transform.forward, Vector3.up).normalized;
        Vector3 direita = Vector3.Cross(Vector3.up, frente).normalized;
        Vector3 movimento = (frente * entrada.y + direita * entrada.x) * velocidade;
        if (controlador.isGrounded && queda < 0f) queda = -2f;
        queda = Mathf.Max(queda + Mathf.Min(-0.1f, gravidade) * Time.deltaTime, -40f);
        Vector3 antes = transform.position;
        controlador.Move((movimento + Vector3.up * queda) * Time.deltaTime);

        float distancia = Vector3.ProjectOnPlane(transform.position - antes, Vector3.up).magnitude;
        Vector3 alvo = Vector3.zero;
        if (entrada.sqrMagnitude > 0.01f && distancia > 0.0001f && controlador.isGrounded)
        {
            fasePasso += distancia * Mathf.PI / Mathf.Max(0.1f, distanciaEntrePassos);
            alvo = new Vector3(Mathf.Sin(fasePasso) * balancoLateral,
                Mathf.Abs(Mathf.Sin(fasePasso)) * alturaBalanco, 0f);
        }
        SuavizarBalanco(alvo);
    }

    private void SuavizarBalanco(Vector3 alvo)
    {
        if (cameraJogador == null) return;
        balancoAtual = Vector3.Lerp(balancoAtual, alvo, 1f - Mathf.Exp(-18f * Time.unscaledDeltaTime));
        if (Time.timeScale <= 0) impactoRestante = 0;
        impactoRestante = Mathf.Max(0, impactoRestante - Time.deltaTime);
        float peso = Mathf.Pow(impactoRestante / .12f, 2) * intensidadeImpacto * forcaImpacto;
        Vector3 impacto = new Vector3(ladoImpacto * .018f, -.022f, -.060f) * peso;
        cameraJogador.localPosition = posicaoOriginalCamera + balancoAtual + impacto;
    }

    public void AplicarImpacto(float intensidade, bool esquerda)
    {
        if (!iniciou || Time.timeScale <= 0) return;
        impactoRestante = .12f;
        intensidadeImpacto = Mathf.Clamp01(intensidade);
        ladoImpacto = esquerda ? -1 : 1;
    }
    private void CapturarMouse()
    {
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
        pularDelta = true;
    }
    private void LiberarMouse()
    {
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }
    void OnApplicationFocus(bool foco)
    {
        if (!foco && iniciou) LiberarMouse();
        pularDelta = true;
    }
    void OnDisable()
    {
        if (!iniciou) return;
        Cursor.lockState = lockAnterior;
        Cursor.visible = visivelAnterior;
        if (cameraJogador != null)
        {
            cameraJogador.localPosition = posicaoOriginalCamera;
            cameraJogador.localRotation = rotacaoOriginalCamera;
        }
        balancoAtual = Vector3.zero;
        impactoRestante = 0;
        inclinacao = 0f;
    }
}
