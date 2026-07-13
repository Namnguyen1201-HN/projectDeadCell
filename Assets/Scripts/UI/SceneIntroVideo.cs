using UnityEngine;
using UnityEngine.Video;

public class SceneIntroVideo : MonoBehaviour
{
    [Header("Video Settings")]
    public VideoPlayer videoPlayer;
    
    [Tooltip("UI Panel chứa RawImage để hiển thị Video")]
    public GameObject videoPanel; 
    
    [Tooltip("Có muốn dừng thời gian trong game khi video đang phát không?")]
    public bool pauseGameDuringVideo = true;

    [Tooltip("Phím để bỏ qua video")]
    public KeyCode skipKey = KeyCode.Space;
    public KeyCode altSkipKey = KeyCode.Escape;

    private void Start()
    {
        // Kiểm tra xem có video player và panel không
        if (videoPlayer != null && videoPanel != null)
        {
            videoPanel.SetActive(true);
            
            if (pauseGameDuringVideo)
            {
                Time.timeScale = 0f; // Dừng mọi hoạt động của game
            }

            // Bắt sự kiện khi video chạy xong
            videoPlayer.loopPointReached += OnVideoEnd;
            videoPlayer.Play();
        }
        else
        {
            Debug.LogWarning("SceneIntroVideo: Thiếu VideoPlayer hoặc VideoPanel!");
            if (videoPanel != null) videoPanel.SetActive(false);
        }
    }

    private void Update()
    {
        // Cho phép bấm phím để bỏ qua video
        if (videoPanel != null && videoPanel.activeSelf)
        {
            if (Input.GetKeyDown(skipKey) || Input.GetKeyDown(altSkipKey))
            {
                OnVideoEnd(videoPlayer);
            }
        }
    }

    private void OnVideoEnd(VideoPlayer vp)
    {
        if (vp != null)
        {
            vp.Stop();
            vp.loopPointReached -= OnVideoEnd;
        }
        
        if (pauseGameDuringVideo)
        {
            Time.timeScale = 1f; // Khôi phục thời gian game
        }

        if (videoPanel != null)
        {
            videoPanel.SetActive(false); // Ẩn video đi để bắt đầu chơi
        }
    }
}
