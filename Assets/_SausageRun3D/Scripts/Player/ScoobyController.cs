using UnityEngine;

#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

[RequireComponent(typeof(CharacterController))]
public class ScoobyController : MonoBehaviour
{
    [Header("Camera")]
    [SerializeField] private Camera cameraScooby;
    [SerializeField] private float sensibilidadeMouse = 0.12f;

    [Header("Movimentacao")]
    [SerializeField] private float velocidadeAndando = 2.5f;
    [SerializeField] private float velocidadeCorrendo = 4.0f;
    [SerializeField] private float alturaPulo = 0.6f;
    [SerializeField] private float gravidade = -18f;

    private CharacterController controlador;
    private float velocidadeVertical;
    private float anguloVertical;

    private void Awake()
    {
        controlador = GetComponent<CharacterController>();

        if (cameraScooby == null)
            cameraScooby = GetComponentInChildren<Camera>();

        if (cameraScooby == null)
            Debug.LogError("Scooby: crie a CameraScooby como filha do jogador.", this);
    }

    private void Start()
    {
        TravarMouse();

#if !ENABLE_INPUT_SYSTEM && !ENABLE_LEGACY_INPUT_MANAGER
        Debug.LogError("Nenhum sistema de entrada ativo. Verifique Player > Active Input Handling.", this);
#endif
    }

    private void Update()
    {
        float horizontal = 0f;
        float vertical = 0f;
        float olharX = 0f;
        float olharY = 0f;
        bool correndo = false;
        bool pular = false;
        bool escapar = false;
        bool clicar = false;

#if ENABLE_INPUT_SYSTEM
        Keyboard teclado = Keyboard.current;
        Mouse mouse = Mouse.current;

        if (teclado != null)
        {
            horizontal = (teclado.dKey.isPressed ? 1f : 0f) - (teclado.aKey.isPressed ? 1f : 0f);
            vertical = (teclado.wKey.isPressed ? 1f : 0f) - (teclado.sKey.isPressed ? 1f : 0f);
            correndo = teclado.leftShiftKey.isPressed || teclado.rightShiftKey.isPressed;
            pular = teclado.spaceKey.wasPressedThisFrame;
            escapar = teclado.escapeKey.wasPressedThisFrame;
        }

        if (mouse != null)
        {
            Vector2 delta = mouse.delta.ReadValue();
            olharX = delta.x;
            olharY = delta.y;
            clicar = mouse.leftButton.wasPressedThisFrame;
        }
#elif ENABLE_LEGACY_INPUT_MANAGER
        horizontal = Input.GetAxisRaw("Horizontal");
        vertical = Input.GetAxisRaw("Vertical");
        correndo = Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift);
        pular = Input.GetKeyDown(KeyCode.Space);
        escapar = Input.GetKeyDown(KeyCode.Escape);
        clicar = Input.GetMouseButtonDown(0);
        olharX = Input.GetAxis("Mouse X") * 10f;
        olharY = Input.GetAxis("Mouse Y") * 10f;
#endif

        if (escapar)
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }
        else if (clicar && Cursor.lockState != CursorLockMode.Locked)
        {
            TravarMouse();
        }

        if (Cursor.lockState == CursorLockMode.Locked && cameraScooby != null)
        {
            transform.Rotate(0f, olharX * sensibilidadeMouse, 0f);
            anguloVertical = Mathf.Clamp(anguloVertical - olharY * sensibilidadeMouse, -80f, 80f);
            cameraScooby.transform.localRotation = Quaternion.Euler(anguloVertical, 0f, 0f);
        }

        Vector3 movimento = Vector3.ClampMagnitude(
            transform.right * horizontal + transform.forward * vertical, 1f);
        float velocidadeAtual = correndo ? velocidadeCorrendo : velocidadeAndando;

        if (controlador.isGrounded && velocidadeVertical < 0f)
            velocidadeVertical = -2f;

        if (pular && controlador.isGrounded)
            velocidadeVertical = Mathf.Sqrt(alturaPulo * -2f * gravidade);

        velocidadeVertical += gravidade * Time.deltaTime;

        Vector3 deslocamento = movimento * velocidadeAtual;
        deslocamento.y = velocidadeVertical;
        controlador.Move(deslocamento * Time.deltaTime);
    }

    private static void TravarMouse()
    {
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }
}
