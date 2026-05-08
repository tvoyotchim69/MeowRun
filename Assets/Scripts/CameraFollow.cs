using UnityEngine;

public class CameraFollow : MonoBehaviour
{
    public Transform target;       // —сылка на трансформ игрока
    public Vector3 offset;         // –ассто€ние между камерой и игроком
    public float smoothSpeed = 5f; // Ќасколько плавно камера будет догон€ть игрока

    void Start()
    {
        // ≈сли offset не задан вручную, вычисл€ем его автоматически при старте
        if (target != null && offset == Vector3.zero)
        {
            offset = transform.position - target.position;
        }
    }

    void LateUpdate()
    {
        if (target == null) return;

        // ќпредел€ем желаемую позицию (только вперед по Z и по сторонам по X)
        // ≈сли вы хотите, чтобы камера Ќ≈ двигалась за игроком влево-вправо, 
        // замените target.position.x на 0.
        Vector3 desiredPosition = target.position + offset;

        // ѕлавно перемещаем камеру из текущей позиции в желаемую
        Vector3 smoothedPosition = Vector3.Lerp(transform.position, desiredPosition, smoothSpeed * Time.deltaTime);

        transform.position = smoothedPosition;
    }
}