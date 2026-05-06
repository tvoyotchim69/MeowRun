using UnityEngine;

public class Coin : MonoBehaviour
{
    public int coinValue = 1;
    public AudioClip collectSound; // Поле для аудиофайла

    void OnTriggerEnter(Collider other)
    {
        // Проверяем, что объект, который вошел в триггер, имеет тег "Player"
        if (other.CompareTag("Player"))
        {
            // Добавляем монету в счетчик через Singleton
            if (CoinCounter.instance != null)
            {
                CoinCounter.instance.AddCoins(coinValue);
            }

            // Воспроизводим звук в позиции монеты перед удалением
            if (collectSound != null)
            {
                // Метод создает временный объект для звука, который не удалится мгновенно
                AudioSource.PlayClipAtPoint(collectSound, transform.position);
            }

            // Уничтожаем объект монеты
            Destroy(gameObject);
        }
    }
}