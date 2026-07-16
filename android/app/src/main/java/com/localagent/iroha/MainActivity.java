package com.localagent.iroha;

import android.app.Activity;
import android.app.AlertDialog;
import android.content.Context;
import android.content.SharedPreferences;
import android.graphics.Bitmap;
import android.graphics.BitmapFactory;
import android.graphics.Canvas;
import android.graphics.Color;
import android.graphics.LinearGradient;
import android.graphics.Paint;
import android.graphics.Path;
import android.graphics.Rect;
import android.graphics.RectF;
import android.graphics.Shader;
import android.graphics.drawable.GradientDrawable;
import android.media.MediaPlayer;
import android.os.Bundle;
import android.os.Handler;
import android.os.Looper;
import android.text.InputType;
import android.view.Gravity;
import android.view.View;
import android.view.inputmethod.InputMethodManager;
import android.widget.Button;
import android.widget.CheckBox;
import android.widget.EditText;
import android.widget.LinearLayout;
import android.widget.ScrollView;
import android.widget.TextView;

import org.json.JSONArray;
import org.json.JSONObject;

import java.io.ByteArrayOutputStream;
import java.io.File;
import java.io.FileOutputStream;
import java.io.InputStream;
import java.io.OutputStream;
import java.net.HttpURLConnection;
import java.net.URLEncoder;
import java.net.URL;
import java.nio.charset.StandardCharsets;
import java.util.ArrayList;
import java.util.List;

public class MainActivity extends Activity {
    private static final int STATE_IDLE = 0;
    private static final int STATE_THINKING = 1;
    private static final int STATE_SPEAKING = 2;
    private static final int STATE_HAPPY = 3;
    private static final int STATE_ERROR = 4;
    private static final String DEFAULT_VOICE_REF_AUDIO_PATH =
            "voices/iroha/ref.wav";
    private static final String DEFAULT_VOICE_PROMPT_TEXT = "さすがにここは危ないかもいや、警察に届けるか";
    private static final String DEFAULT_VOICE_PROMPT_LANG = "ja";

    private final Handler mainHandler = new Handler(Looper.getMainLooper());
    private final ArrayList<Message> history = new ArrayList<>();
    private final ArrayList<String> feedbackLines = new ArrayList<>();

    private SharedPreferences prefs;
    private Settings settings;
    private AvatarView avatar;
    private LinearLayout chatList;
    private ScrollView chatScroll;
    private TextView statusView;
    private TextView feedbackView;
    private EditText inputBox;
    private Button sendButton;
    private Button settingsButton;
    private Button voiceButton;

    @Override
    protected void onCreate(Bundle savedInstanceState) {
        super.onCreate(savedInstanceState);
        prefs = getSharedPreferences("iroha-agent-settings", MODE_PRIVATE);
        settings = Settings.load(prefs);
        buildUi();
        addFeedback("应用已启动");
        addFeedback("Android 端已启用移动布局");
        addAssistantBubble("你好，我是本地聊天 Agent。请先在设置里填写 DeepSeek API Key；语音服务可填写电脑的局域网地址。");
        setStatus("空闲", STATE_IDLE);
    }

    private void buildUi() {
        LinearLayout root = new LinearLayout(this);
        root.setOrientation(LinearLayout.VERTICAL);
        root.setBackgroundColor(Color.rgb(242, 250, 253));
        root.setPadding(dp(12), dp(10), dp(12), dp(10));
        setContentView(root);

        LinearLayout topBar = new LinearLayout(this);
        topBar.setOrientation(LinearLayout.HORIZONTAL);
        topBar.setGravity(Gravity.CENTER_VERTICAL);
        root.addView(topBar, new LinearLayout.LayoutParams(
                LinearLayout.LayoutParams.MATCH_PARENT, dp(44)));

        TextView title = new TextView(this);
        title.setText("本地聊天 Agent");
        title.setTextSize(20);
        title.setTextColor(Color.rgb(35, 42, 56));
        title.setGravity(Gravity.CENTER_VERTICAL);
        title.setTypeface(null, 1);
        topBar.addView(title, new LinearLayout.LayoutParams(0,
                LinearLayout.LayoutParams.MATCH_PARENT, 1));

        settingsButton = makeButton("设置");
        settingsButton.setOnClickListener(v -> showSettingsDialog());
        topBar.addView(settingsButton, new LinearLayout.LayoutParams(dp(74), dp(38)));

        voiceButton = makeButton("试音");
        voiceButton.setOnClickListener(v -> testVoice());
        LinearLayout.LayoutParams voiceParams = new LinearLayout.LayoutParams(dp(74), dp(38));
        voiceParams.leftMargin = dp(8);
        topBar.addView(voiceButton, voiceParams);

        avatar = new AvatarView(this);
        LinearLayout.LayoutParams avatarParams = new LinearLayout.LayoutParams(
                LinearLayout.LayoutParams.MATCH_PARENT, dp(238));
        avatarParams.topMargin = dp(8);
        root.addView(avatar, avatarParams);

        statusView = new TextView(this);
        statusView.setTextSize(14);
        statusView.setTextColor(Color.rgb(47, 62, 82));
        statusView.setPadding(dp(2), dp(7), dp(2), dp(5));
        root.addView(statusView, new LinearLayout.LayoutParams(
                LinearLayout.LayoutParams.MATCH_PARENT, dp(34)));

        feedbackView = new TextView(this);
        feedbackView.setTextSize(12);
        feedbackView.setTextColor(Color.rgb(80, 85, 96));
        feedbackView.setBackground(roundedBg(Color.rgb(253, 255, 255), 14));
        feedbackView.setPadding(dp(10), dp(8), dp(10), dp(8));
        root.addView(feedbackView, new LinearLayout.LayoutParams(
                LinearLayout.LayoutParams.MATCH_PARENT, dp(76)));

        chatScroll = new ScrollView(this);
        chatScroll.setFillViewport(false);
        chatScroll.setBackground(roundedStrokeBg(Color.rgb(253, 255, 255), 16, Color.rgb(206, 236, 243)));
        chatList = new LinearLayout(this);
        chatList.setOrientation(LinearLayout.VERTICAL);
        chatList.setPadding(dp(8), dp(8), dp(8), dp(8));
        chatScroll.addView(chatList, new ScrollView.LayoutParams(
                ScrollView.LayoutParams.MATCH_PARENT, ScrollView.LayoutParams.WRAP_CONTENT));
        LinearLayout.LayoutParams chatParams = new LinearLayout.LayoutParams(
                LinearLayout.LayoutParams.MATCH_PARENT, 0, 1);
        chatParams.topMargin = dp(8);
        root.addView(chatScroll, chatParams);

        LinearLayout inputRow = new LinearLayout(this);
        inputRow.setOrientation(LinearLayout.HORIZONTAL);
        inputRow.setGravity(Gravity.CENTER_VERTICAL);
        LinearLayout.LayoutParams inputRowParams = new LinearLayout.LayoutParams(
                LinearLayout.LayoutParams.MATCH_PARENT, dp(62));
        inputRowParams.topMargin = dp(8);
        root.addView(inputRow, inputRowParams);

        inputBox = new EditText(this);
        inputBox.setHint("输入消息");
        inputBox.setTextSize(16);
        inputBox.setSingleLine(false);
        inputBox.setMinLines(1);
        inputBox.setMaxLines(3);
        inputBox.setInputType(InputType.TYPE_CLASS_TEXT | InputType.TYPE_TEXT_FLAG_MULTI_LINE);
        inputRow.addView(inputBox, new LinearLayout.LayoutParams(0,
                LinearLayout.LayoutParams.MATCH_PARENT, 1));

        sendButton = makeButton("发送");
        sendButton.setTextSize(16);
        sendButton.setOnClickListener(v -> sendMessage());
        LinearLayout.LayoutParams sendParams = new LinearLayout.LayoutParams(dp(86),
                LinearLayout.LayoutParams.MATCH_PARENT);
        sendParams.leftMargin = dp(8);
        inputRow.addView(sendButton, sendParams);
    }

    private Button makeButton(String text) {
        Button button = new Button(this);
        button.setText(text);
        button.setAllCaps(false);
        button.setTextColor(Color.WHITE);
        button.setBackground(roundedBg(Color.rgb(72, 169, 196), 12));
        return button;
    }

    private GradientDrawable roundedBg(int color, int radiusDp) {
        GradientDrawable drawable = new GradientDrawable();
        drawable.setColor(color);
        drawable.setCornerRadius(dp(radiusDp));
        return drawable;
    }

    private GradientDrawable roundedStrokeBg(int color, int radiusDp, int strokeColor) {
        GradientDrawable drawable = roundedBg(color, radiusDp);
        drawable.setStroke(dp(1), strokeColor);
        return drawable;
    }

    private void showSettingsDialog() {
        LinearLayout box = new LinearLayout(this);
        box.setOrientation(LinearLayout.VERTICAL);
        box.setPadding(dp(10), dp(4), dp(10), dp(4));

        EditText apiKey = makeField("DeepSeek API Key", settings.apiKey, true);
        EditText baseUrl = makeField("接口地址", settings.baseUrl, false);
        EditText model = makeField("模型", settings.model, false);
        EditText voiceUrl = makeField("语音服务地址", settings.voiceServerUrl, false);
        EditText voiceRef = makeField("参考音频路径", settings.voiceRefAudioPath, false);
        EditText voicePrompt = makeField("参考文本", settings.voicePromptText, false);
        EditText voicePromptLang = makeField("参考语种", settings.voicePromptLang, false);
        CheckBox voiceEnabled = new CheckBox(this);
        voiceEnabled.setText("启用日语语音输出");
        voiceEnabled.setChecked(settings.voiceEnabled);

        TextView note = new TextView(this);
        note.setText("安卓真机不能用 127.0.0.1 访问电脑，请填写电脑局域网 IP，例如 http://192.168.1.23:9880");
        note.setTextSize(12);
        note.setTextColor(Color.rgb(90, 90, 100));
        note.setPadding(0, dp(8), 0, 0);

        box.addView(apiKey);
        box.addView(baseUrl);
        box.addView(model);
        box.addView(voiceUrl);
        box.addView(voiceRef);
        box.addView(voicePrompt);
        box.addView(voicePromptLang);
        box.addView(voiceEnabled);
        box.addView(note);

        new AlertDialog.Builder(this)
                .setTitle("设置")
                .setView(box)
                .setNegativeButton("取消", null)
                .setPositiveButton("保存", (dialog, which) -> {
                    settings.apiKey = apiKey.getText().toString().trim();
                    settings.baseUrl = valueOrDefault(baseUrl.getText().toString(), "https://api.deepseek.com");
                    settings.model = valueOrDefault(model.getText().toString(), "deepseek-v4-flash");
                    settings.voiceServerUrl = voiceUrl.getText().toString().trim();
                    settings.voiceRefAudioPath = valueOrDefault(voiceRef.getText().toString(), DEFAULT_VOICE_REF_AUDIO_PATH);
                    settings.voicePromptText = valueOrDefault(voicePrompt.getText().toString(), DEFAULT_VOICE_PROMPT_TEXT);
                    settings.voicePromptLang = valueOrDefault(voicePromptLang.getText().toString(), DEFAULT_VOICE_PROMPT_LANG);
                    settings.voiceEnabled = voiceEnabled.isChecked();
                    settings.save(prefs);
                    addFeedback("设置已保存");
                    setStatus("设置已保存", STATE_HAPPY);
                })
                .show();
    }

    private EditText makeField(String hint, String value, boolean password) {
        EditText field = new EditText(this);
        field.setHint(hint);
        field.setText(value == null ? "" : value);
        field.setSingleLine(true);
        field.setTextSize(14);
        if (password) {
            field.setInputType(InputType.TYPE_CLASS_TEXT | InputType.TYPE_TEXT_VARIATION_PASSWORD);
        }
        return field;
    }

    private void sendMessage() {
        String text = inputBox.getText().toString().trim();
        if (text.length() == 0) {
            addFeedback("输入为空，未发送");
            setStatus("请输入内容", STATE_ERROR);
            return;
        }

        hideKeyboard();
        inputBox.setText("");
        addUserBubble(text);
        history.add(new Message("user", text));
        trimHistory();

        setBusy(true);
        new Thread(() -> {
            try {
                if (settings.apiKey == null || settings.apiKey.trim().length() == 0) {
                    postUi(() -> {
                        addFeedback("缺少 DeepSeek API Key");
                        setStatus("请先填写 API Key", STATE_ERROR);
                        addAssistantBubble("还没有配置 DeepSeek API Key。请点右上角“设置”填写。");
                    });
                    return;
                }

                postUi(() -> {
                    addFeedback("发送请求到 " + settings.baseUrl);
                    setStatus("正在请求 DeepSeek", STATE_THINKING);
                });

                AgentReply reply = requestDeepSeek();
                history.add(new Message("assistant", reply.zh));
                trimHistory();

                postUi(() -> {
                    addFeedback("已收到模型回复");
                    addAssistantBubble(reply.zh);
                    setStatus("中文回复已显示", reply.mood);
                });

                if (settings.voiceEnabled) {
                    playVoice(reply.ja);
                } else {
                    postUi(() -> addFeedback("语音已关闭，跳过发声"));
                }

                postUi(() -> setStatus("完成", STATE_HAPPY));
            } catch (Exception ex) {
                postUi(() -> {
                    addFeedback("错误: " + ex.getMessage());
                    setStatus("发生错误", STATE_ERROR);
                    addAssistantBubble("操作失败：" + ex.getMessage());
                });
            } finally {
                postUi(() -> setBusy(false));
            }
        }).start();
    }

    private AgentReply requestDeepSeek() throws Exception {
        JSONArray messages = new JSONArray();
        JSONObject system = new JSONObject();
        system.put("role", "system");
        system.put("content", buildSystemPrompt());
        messages.put(system);

        for (Message item : history) {
            JSONObject msg = new JSONObject();
            msg.put("role", item.role);
            msg.put("content", item.content);
            messages.put(msg);
        }

        JSONObject payload = new JSONObject();
        payload.put("model", settings.model);
        payload.put("temperature", 0.7);
        payload.put("messages", messages);

        String url = trimEnd(settings.baseUrl, "/") + "/chat/completions";
        String response = postJson(url, payload.toString(), settings.apiKey);
        JSONObject root = new JSONObject(response);
        JSONArray choices = root.getJSONArray("choices");
        String content = choices.getJSONObject(0)
                .getJSONObject("message")
                .getString("content");
        return parseAgentReply(content);
    }

    private String buildSystemPrompt() {
        return "你是一个本地聊天 Agent。用户用中文聊天。你必须只输出严格 JSON，不要 Markdown，不要代码块。"
                + "JSON 格式为 {\"zh\":\"中文聊天框回复\",\"ja\":\"自然日语语音台词\",\"mood\":\"idle|thinking|speaking|happy|error\"}。"
                + "zh 必须是中文，显示给用户看。ja 必须是日语，供语音朗读。mood 根据情绪选择。";
    }

    private String postJson(String urlText, String json, String apiKey) throws Exception {
        HttpURLConnection conn = (HttpURLConnection) new URL(urlText).openConnection();
        conn.setRequestMethod("POST");
        conn.setConnectTimeout(20000);
        conn.setReadTimeout(90000);
        conn.setDoOutput(true);
        conn.setRequestProperty("Content-Type", "application/json; charset=utf-8");
        if (apiKey != null && apiKey.length() > 0) {
            conn.setRequestProperty("Authorization", "Bearer " + apiKey.trim());
        }

        byte[] body = json.getBytes(StandardCharsets.UTF_8);
        conn.setFixedLengthStreamingMode(body.length);
        try (OutputStream os = conn.getOutputStream()) {
            os.write(body);
        }

        int status = conn.getResponseCode();
        InputStream stream = status >= 400 ? conn.getErrorStream() : conn.getInputStream();
        byte[] bytes = readAll(stream);
        String text = new String(bytes, StandardCharsets.UTF_8);
        if (status >= 400) {
            throw new IllegalStateException("HTTP " + status + ": " + limit(text, 360));
        }
        return text;
    }

    private AgentReply parseAgentReply(String raw) {
        try {
            String json = stripToJson(raw);
            JSONObject obj = new JSONObject(json);
            return new AgentReply(
                    obj.optString("zh", raw),
                    obj.optString("ja", "すみません、音声用の返答を作れませんでした。"),
                    parseMood(obj.optString("mood", "happy")));
        } catch (Exception ex) {
            postUi(() -> addFeedback("模型未返回标准 JSON，已使用文本兜底"));
            return new AgentReply(raw, "すみません、返答の形式を整えられませんでした。", STATE_HAPPY);
        }
    }

    private void playVoice(String japaneseText) throws Exception {
        if (japaneseText == null || japaneseText.trim().length() == 0) {
            postUi(() -> addFeedback("日语台词为空，跳过语音"));
            return;
        }
        if (settings.voiceServerUrl == null || settings.voiceServerUrl.trim().length() == 0) {
            postUi(() -> addFeedback("未配置语音服务地址，跳过发声"));
            return;
        }

        postUi(() -> {
            addFeedback("语音台词: " + limit(japaneseText, 70));
            setStatus("正在请求语音服务", STATE_SPEAKING);
        });

        byte[] audio = requestVoiceAudio(japaneseText);
        if (audio == null || audio.length < 44) {
            postUi(() -> addFeedback("语音服务没有返回可播放音频"));
            return;
        }

        postUi(() -> setStatus("正在播放语音", STATE_SPEAKING));
        File file = File.createTempFile("iroha-agent-voice-", ".wav", getCacheDir());
        try (FileOutputStream fos = new FileOutputStream(file)) {
            fos.write(audio);
        }

        MediaPlayer player = new MediaPlayer();
        try {
            player.setDataSource(file.getAbsolutePath());
            player.prepare();
            player.start();
            postUi(() -> addFeedback("开始播放语音"));
            while (player.isPlaying()) {
                Thread.sleep(120);
            }
            postUi(() -> addFeedback("语音播放完成"));
        } finally {
            player.release();
            //noinspection ResultOfMethodCallIgnored
            file.delete();
        }
    }

    private byte[] requestVoiceAudio(String japaneseText) throws Exception {
        String endpoint = buildTtsEndpoint(settings.voiceServerUrl);
        String promptText = valueOrDefault(settings.voicePromptText, DEFAULT_VOICE_PROMPT_TEXT);
        String promptLang = valueOrDefault(settings.voicePromptLang, DEFAULT_VOICE_PROMPT_LANG);
        String refAudioPath = valueOrDefault(settings.voiceRefAudioPath, DEFAULT_VOICE_REF_AUDIO_PATH);
        try {
            JSONObject payload = new JSONObject();
            payload.put("text", japaneseText);
            payload.put("text_lang", "ja");
            payload.put("prompt_text", promptText);
            payload.put("prompt_lang", promptLang);
            payload.put("ref_audio_path", refAudioPath);
            payload.put("text_split_method", "cut5");
            payload.put("batch_size", 1);
            payload.put("speed_factor", 1.0);
            payload.put("streaming_mode", false);
            payload.put("media_type", "wav");
            byte[] bytes = postForBytes(endpoint, payload.toString());
            if (looksLikeAudio(bytes)) return bytes;
            postUi(() -> addFeedback("POST /tts 未返回音频"));
        } catch (Exception ex) {
            postUi(() -> addFeedback("POST /tts 失败: " + ex.getMessage()));
        }

        String url = endpoint
                + "?text=" + URLEncoder.encode(japaneseText, "UTF-8")
                + "&text_lang=ja"
                + "&prompt_text=" + URLEncoder.encode(promptText, "UTF-8")
                + "&prompt_lang=" + URLEncoder.encode(promptLang, "UTF-8")
                + "&ref_audio_path=" + URLEncoder.encode(refAudioPath, "UTF-8")
                + "&media_type=wav&streaming_mode=false";
        byte[] bytes = getBytes(url);
        if (looksLikeAudio(bytes)) return bytes;
        postUi(() -> addFeedback("GET /tts 未返回音频"));
        return null;
    }

    private byte[] postForBytes(String urlText, String json) throws Exception {
        HttpURLConnection conn = (HttpURLConnection) new URL(urlText).openConnection();
        conn.setRequestMethod("POST");
        conn.setConnectTimeout(20000);
        conn.setReadTimeout(90000);
        conn.setDoOutput(true);
        conn.setRequestProperty("Content-Type", "application/json; charset=utf-8");
        byte[] body = json.getBytes(StandardCharsets.UTF_8);
        conn.setFixedLengthStreamingMode(body.length);
        try (OutputStream os = conn.getOutputStream()) {
            os.write(body);
        }

        int status = conn.getResponseCode();
        InputStream stream = status >= 400 ? conn.getErrorStream() : conn.getInputStream();
        byte[] bytes = readAll(stream);
        if (status >= 400) {
            throw new IllegalStateException("HTTP " + status + ": " + limit(new String(bytes, StandardCharsets.UTF_8), 180));
        }
        return bytes;
    }

    private byte[] getBytes(String urlText) throws Exception {
        HttpURLConnection conn = (HttpURLConnection) new URL(urlText).openConnection();
        conn.setRequestMethod("GET");
        conn.setConnectTimeout(20000);
        conn.setReadTimeout(90000);
        int status = conn.getResponseCode();
        InputStream stream = status >= 400 ? conn.getErrorStream() : conn.getInputStream();
        byte[] bytes = readAll(stream);
        if (status >= 400) {
            throw new IllegalStateException("HTTP " + status + ": " + limit(new String(bytes, StandardCharsets.UTF_8), 180));
        }
        return bytes;
    }

    private boolean looksLikeAudio(byte[] bytes) {
        if (bytes == null || bytes.length < 44) return false;
        boolean wav = bytes[0] == 'R' && bytes[1] == 'I' && bytes[2] == 'F' && bytes[3] == 'F';
        boolean mp3 = bytes[0] == 'I' && bytes[1] == 'D' && bytes[2] == '3';
        return wav || mp3 || bytes.length > 4096;
    }

    private byte[] readAll(InputStream stream) throws Exception {
        if (stream == null) return new byte[0];
        ByteArrayOutputStream out = new ByteArrayOutputStream();
        byte[] buffer = new byte[8192];
        int read;
        while ((read = stream.read(buffer)) != -1) {
            out.write(buffer, 0, read);
        }
        stream.close();
        return out.toByteArray();
    }

    private void testVoice() {
        setBusy(true);
        new Thread(() -> {
            try {
                postUi(() -> addFeedback("开始测试语音服务"));
                playVoice("こんにちは。音声サービスの接続テストです。");
                postUi(() -> setStatus("语音测试完成", STATE_HAPPY));
            } catch (Exception ex) {
                postUi(() -> {
                    addFeedback("语音测试失败: " + ex.getMessage());
                    setStatus("语音测试失败", STATE_ERROR);
                });
            } finally {
                postUi(() -> setBusy(false));
            }
        }).start();
    }

    private void addUserBubble(String text) {
        addBubble("你", text, Color.rgb(227, 238, 255), Color.rgb(35, 82, 142), Gravity.RIGHT);
    }

    private void addAssistantBubble(String text) {
        addBubble("Agent", text, Color.rgb(244, 237, 249), Color.rgb(92, 54, 126), Gravity.LEFT);
    }

    private void addBubble(String role, String text, int background, int labelColor, int gravity) {
        TextView bubble = new TextView(this);
        bubble.setText(role + "\n" + text);
        bubble.setTextSize(15);
        bubble.setTextColor(Color.rgb(40, 43, 50));
        bubble.setPadding(dp(12), dp(9), dp(12), dp(9));
        bubble.setBackground(roundedStrokeBg(background, 16, labelColor));
        bubble.setLineSpacing(dp(2), 1.0f);

        LinearLayout.LayoutParams params = new LinearLayout.LayoutParams(
                LinearLayout.LayoutParams.WRAP_CONTENT, LinearLayout.LayoutParams.WRAP_CONTENT);
        params.setMargins(dp(4), dp(5), dp(4), dp(5));
        params.gravity = gravity;
        params.width = Math.min(getResources().getDisplayMetrics().widthPixels - dp(54), dp(560));
        chatList.addView(bubble, params);
        chatScroll.post(() -> chatScroll.fullScroll(View.FOCUS_DOWN));
    }

    private void addFeedback(String text) {
        String line = nowTime() + "  " + text;
        feedbackLines.add(line);
        while (feedbackLines.size() > 4) {
            feedbackLines.remove(0);
        }
        StringBuilder builder = new StringBuilder();
        for (String item : feedbackLines) {
            if (builder.length() > 0) builder.append('\n');
            builder.append(item);
        }
        feedbackView.setText(builder.toString());
    }

    private void setStatus(String text, int state) {
        statusView.setText("状态: " + text);
        avatar.setState(state);
    }

    private void setBusy(boolean busy) {
        sendButton.setEnabled(!busy);
        settingsButton.setEnabled(!busy);
        voiceButton.setEnabled(!busy);
    }

    private void postUi(Runnable action) {
        mainHandler.post(action);
    }

    private void hideKeyboard() {
        InputMethodManager manager = (InputMethodManager) getSystemService(INPUT_METHOD_SERVICE);
        if (manager != null) {
            manager.hideSoftInputFromWindow(inputBox.getWindowToken(), 0);
        }
    }

    private void trimHistory() {
        while (history.size() > 16) {
            history.remove(0);
        }
    }

    private String stripToJson(String raw) {
        if (raw == null) return "{}";
        String text = raw.trim();
        if (text.startsWith("```")) {
            int firstLine = text.indexOf('\n');
            int lastFence = text.lastIndexOf("```");
            if (firstLine >= 0 && lastFence > firstLine) {
                text = text.substring(firstLine + 1, lastFence).trim();
            }
        }
        int start = text.indexOf('{');
        int end = text.lastIndexOf('}');
        if (start >= 0 && end > start) {
            return text.substring(start, end + 1);
        }
        return text;
    }

    private int parseMood(String mood) {
        if ("thinking".equalsIgnoreCase(mood)) return STATE_THINKING;
        if ("speaking".equalsIgnoreCase(mood)) return STATE_SPEAKING;
        if ("happy".equalsIgnoreCase(mood)) return STATE_HAPPY;
        if ("error".equalsIgnoreCase(mood)) return STATE_ERROR;
        return STATE_IDLE;
    }

    private String buildTtsEndpoint(String baseUrl) {
        String clean = trimEnd(baseUrl.trim(), "/");
        if (clean.endsWith("/tts")) return clean;
        return clean + "/tts";
    }

    private String trimEnd(String value, String suffix) {
        String result = value;
        while (result.endsWith(suffix)) {
            result = result.substring(0, result.length() - suffix.length());
        }
        return result;
    }

    private String valueOrDefault(String value, String fallback) {
        if (value == null || value.trim().length() == 0) return fallback;
        return value.trim();
    }

    private String limit(String text, int length) {
        if (text == null) return "";
        return text.length() <= length ? text : text.substring(0, length) + "...";
    }

    private String nowTime() {
        java.text.SimpleDateFormat format = new java.text.SimpleDateFormat("HH:mm:ss", java.util.Locale.CHINA);
        return format.format(new java.util.Date());
    }

    private int dp(int value) {
        return (int) (value * getResources().getDisplayMetrics().density + 0.5f);
    }

    private static final class Settings {
        String apiKey = "";
        String baseUrl = "https://api.deepseek.com";
        String model = "deepseek-v4-flash";
        String voiceServerUrl = "http://127.0.0.1:9880";
        String voiceRefAudioPath = DEFAULT_VOICE_REF_AUDIO_PATH;
        String voicePromptText = DEFAULT_VOICE_PROMPT_TEXT;
        String voicePromptLang = DEFAULT_VOICE_PROMPT_LANG;
        boolean voiceEnabled = false;

        static Settings load(SharedPreferences prefs) {
            Settings settings = new Settings();
            settings.apiKey = prefs.getString("apiKey", "");
            settings.baseUrl = prefs.getString("baseUrl", "https://api.deepseek.com");
            settings.model = prefs.getString("model", "deepseek-v4-flash");
            settings.voiceServerUrl = prefs.getString("voiceServerUrl", "http://127.0.0.1:9880");
            settings.voiceRefAudioPath = prefs.getString("voiceRefAudioPath", DEFAULT_VOICE_REF_AUDIO_PATH);
            settings.voicePromptText = prefs.getString("voicePromptText", DEFAULT_VOICE_PROMPT_TEXT);
            settings.voicePromptLang = prefs.getString("voicePromptLang", DEFAULT_VOICE_PROMPT_LANG);
            settings.voiceEnabled = prefs.getBoolean("voiceEnabled", false);
            return settings;
        }

        void save(SharedPreferences prefs) {
            prefs.edit()
                    .putString("apiKey", apiKey)
                    .putString("baseUrl", baseUrl)
                    .putString("model", model)
                    .putString("voiceServerUrl", voiceServerUrl)
                    .putString("voiceRefAudioPath", voiceRefAudioPath)
                    .putString("voicePromptText", voicePromptText)
                    .putString("voicePromptLang", voicePromptLang)
                    .putBoolean("voiceEnabled", voiceEnabled)
                    .apply();
        }
    }

    private static final class Message {
        final String role;
        final String content;

        Message(String role, String content) {
            this.role = role;
            this.content = content;
        }
    }

    private static final class AgentReply {
        final String zh;
        final String ja;
        final int mood;

        AgentReply(String zh, String ja, int mood) {
            this.zh = zh;
            this.ja = ja;
            this.mood = mood;
        }
    }

    public static final class AvatarView extends View {
        private final Paint paint = new Paint(Paint.ANTI_ALIAS_FLAG);
        private final Handler handler = new Handler(Looper.getMainLooper());
        private final int[][] frameIds = new int[5][];
        private Bitmap[] activeFrames = new Bitmap[0];
        private int activeFrameState = -1;
        private int frame = 0;
        private int state = STATE_IDLE;

        private final Runnable tick = new Runnable() {
            @Override
            public void run() {
                frame++;
                invalidate();
                handler.postDelayed(this, 140);
            }
        };

        public AvatarView(Context context) {
            super(context);
            collectFrameIds(context);
            switchStateFrames(context, STATE_IDLE);
            handler.post(tick);
        }

        public void setState(int state) {
            if (this.state != state) {
                switchStateFrames(getContext(), state);
            }
            this.state = state;
            invalidate();
        }

        @Override
        protected void onDraw(Canvas canvas) {
            super.onDraw(canvas);
            int w = getWidth();
            int h = getHeight();
            paint.setShader(new LinearGradient(0, 0, 0, h,
                    Color.rgb(246, 250, 255), Color.rgb(255, 248, 252), Shader.TileMode.CLAMP));
            canvas.drawRect(0, 0, w, h, paint);
            paint.setShader(null);
            drawStage(canvas, w, h);

            if (drawPortraitFrame(canvas, w, h)) {
                drawBadge(canvas, w);
                return;
            }

            float cx = w / 2f;
            float scale = Math.min(w / 300f, h / 350f);
            canvas.save();
            canvas.translate(cx, 16 * scale);
            canvas.scale(scale, scale);
            float bob = (float) Math.sin(frame / 4.0) * 3f;
            if (state == STATE_THINKING) bob = (float) Math.sin(frame / 2.0) * 4f;
            if (state == STATE_SPEAKING) bob = (float) Math.sin(frame / 1.5) * 5f;
            canvas.translate(0, bob);
            drawAvatar(canvas);
            canvas.restore();
            drawBadge(canvas, w);
        }

        @Override
        protected void onDetachedFromWindow() {
            handler.removeCallbacks(tick);
            recycleActiveFrames();
            super.onDetachedFromWindow();
        }

        private void collectFrameIds(Context context) {
            frameIds[STATE_IDLE] = collectIds(context, "idle", 8);
            frameIds[STATE_THINKING] = collectIds(context, "thinking", 8);
            frameIds[STATE_SPEAKING] = collectIds(context, "speaking", 8);
            frameIds[STATE_HAPPY] = collectIds(context, "happy", 8);
            frameIds[STATE_ERROR] = collectIds(context, "error", 6);
        }

        private int[] collectIds(Context context, String slug, int count) {
            ArrayList<Integer> ids = new ArrayList<>();
            String packageName = context.getPackageName();
            for (int i = 0; i < count; i++) {
                String name = "iroha_" + slug + "_" + String.format(java.util.Locale.US, "%02d", i);
                int id = context.getResources().getIdentifier(name, "drawable", packageName);
                if (id != 0) {
                    ids.add(id);
                }
            }
            int[] result = new int[ids.size()];
            for (int i = 0; i < ids.size(); i++) {
                result[i] = ids.get(i);
            }
            return result;
        }

        private void switchStateFrames(Context context, int nextState) {
            if (activeFrameState == nextState && activeFrames.length > 0) {
                return;
            }
            recycleActiveFrames();
            activeFrameState = nextState;

            int[] ids = (nextState >= 0 && nextState < frameIds.length) ? frameIds[nextState] : null;
            if ((ids == null || ids.length == 0) && nextState != STATE_IDLE) {
                ids = frameIds[STATE_IDLE];
                activeFrameState = STATE_IDLE;
            }
            if (ids == null || ids.length == 0) {
                activeFrames = new Bitmap[0];
                return;
            }

            ArrayList<Bitmap> loaded = new ArrayList<>();
            BitmapFactory.Options options = new BitmapFactory.Options();
            options.inPreferredConfig = Bitmap.Config.ARGB_8888;
            options.inScaled = false;
            for (int id : ids) {
                Bitmap bitmap = BitmapFactory.decodeResource(context.getResources(), id, options);
                if (bitmap != null) {
                    loaded.add(bitmap);
                }
            }
            activeFrames = loaded.toArray(new Bitmap[0]);
            frame = 0;
        }

        private void recycleActiveFrames() {
            if (activeFrames == null) return;
            for (Bitmap bitmap : activeFrames) {
                if (bitmap != null && !bitmap.isRecycled()) {
                    bitmap.recycle();
                }
            }
            activeFrames = new Bitmap[0];
        }

        private void drawStage(Canvas canvas, int w, int h) {
            RectF card = new RectF(2, 2, w - 2, h - 2);
            paint.setShader(new LinearGradient(0, 0, 0, h,
                    Color.rgb(232, 249, 252), Color.rgb(255, 247, 251), Shader.TileMode.CLAMP));
            paint.setStyle(Paint.Style.FILL);
            canvas.drawRoundRect(card, 28, 28, paint);
            paint.setShader(null);

            paint.setStyle(Paint.Style.STROKE);
            paint.setStrokeWidth(2f);
            paint.setColor(Color.argb(96, 90, 202, 220));
            canvas.drawRoundRect(card, 28, 28, paint);
            paint.setStyle(Paint.Style.FILL);

            paint.setColor(Color.argb(70, 58, 196, 218));
            canvas.drawCircle(w - 58, 34, 5, paint);
            canvas.drawCircle(38, h - 42, 4, paint);
            canvas.drawCircle(w - 118, h - 26, 3, paint);
        }

        private boolean drawPortraitFrame(Canvas canvas, int w, int h) {
            if (activeFrames == null || activeFrames.length == 0) {
                switchStateFrames(getContext(), state);
            }
            if (activeFrames == null || activeFrames.length == 0) {
                return false;
            }

            Bitmap bitmap = activeFrames[frame % activeFrames.length];
            if (bitmap == null || bitmap.isRecycled()) {
                return false;
            }
            Rect src = new Rect(0, 0, bitmap.getWidth(), bitmap.getHeight());
            float availableW = w - 8f;
            float availableH = h - 12f;
            float scale = Math.min(availableW / bitmap.getWidth(), availableH / bitmap.getHeight());
            float bw = bitmap.getWidth() * scale;
            float bh = bitmap.getHeight() * scale;
            RectF dst = new RectF((w - bw) / 2f, h - bh - 4f, (w + bw) / 2f, h - 4f);
            paint.setFilterBitmap(true);
            canvas.drawBitmap(bitmap, src, dst, paint);
            return true;
        }

        private void drawAvatar(Canvas c) {
            paint.setStyle(Paint.Style.FILL);
            paint.setColor(Color.argb(42, 98, 154, 224));
            c.drawOval(new RectF(-92, 5, 92, 189), paint);

            paint.setStyle(Paint.Style.STROKE);
            paint.setStrokeWidth(2f);
            paint.setColor(Color.argb(120, 98, 154, 224));
            c.drawOval(new RectF(-70, -26, 70, 10), paint);
            paint.setStyle(Paint.Style.FILL);

            Path coat = new Path();
            coat.moveTo(-58, 180);
            coat.cubicTo(-92, 245, -80, 320, -46, 330);
            coat.lineTo(46, 330);
            coat.cubicTo(80, 320, 92, 245, 58, 180);
            coat.close();
            paint.setShader(new LinearGradient(0, 170, 0, 330,
                    Color.rgb(72, 93, 138), Color.rgb(142, 96, 164), Shader.TileMode.CLAMP));
            c.drawPath(coat, paint);
            paint.setShader(null);

            paint.setColor(Color.rgb(245, 245, 252));
            Path vest = new Path();
            vest.moveTo(-30, 188);
            vest.lineTo(30, 188);
            vest.lineTo(14, 280);
            vest.lineTo(-14, 280);
            vest.close();
            c.drawPath(vest, paint);

            float armLift = state == STATE_HAPPY ? -16 : state == STATE_SPEAKING ? (frame % 4 < 2 ? -10 : 0) : 0;
            paint.setColor(Color.rgb(72, 93, 138));
            paint.setStrokeWidth(16f);
            paint.setStrokeCap(Paint.Cap.ROUND);
            c.drawLine(-58, 205, -100, 262 + armLift, paint);
            c.drawLine(58, 205, 100, 262 - armLift, paint);
            paint.setStrokeCap(Paint.Cap.BUTT);
            paint.setStyle(Paint.Style.FILL);

            paint.setColor(Color.rgb(255, 225, 213));
            c.drawOval(new RectF(-62, 52, 62, 176), paint);
            c.drawOval(new RectF(-70, 108, -50, 136), paint);
            c.drawOval(new RectF(50, 108, 70, 136), paint);

            Path hair = new Path();
            hair.moveTo(-70, 96);
            hair.cubicTo(-64, 30, 52, 24, 70, 94);
            hair.cubicTo(86, 184, -82, 188, -70, 96);
            hair.close();
            paint.setShader(new LinearGradient(0, 28, 0, 198,
                    Color.rgb(56, 50, 82), Color.rgb(117, 84, 145), Shader.TileMode.CLAMP));
            c.drawPath(hair, paint);
            paint.setShader(null);

            paint.setColor(Color.rgb(83, 64, 120));
            c.drawArc(new RectF(-58, 36, 22, 106), 15, 120, true, paint);
            c.drawArc(new RectF(-8, 32, 64, 108), 55, 115, true, paint);

            paint.setColor(Color.rgb(235, 116, 143));
            Path ribbon = new Path();
            ribbon.moveTo(52, 62);
            ribbon.lineTo(92, 44);
            ribbon.lineTo(82, 86);
            ribbon.close();
            c.drawPath(ribbon, paint);
            c.drawOval(new RectF(47, 58, 65, 76), paint);

            drawFace(c);
        }

        private void drawFace(Canvas c) {
            boolean blink = frame % 34 == 0 || frame % 34 == 1;
            paint.setStrokeWidth(3f);
            paint.setColor(Color.rgb(49, 48, 70));
            paint.setStyle(Paint.Style.STROKE);

            if (state == STATE_ERROR) {
                c.drawLine(-36, 104, -18, 116, paint);
                c.drawLine(-18, 104, -36, 116, paint);
                c.drawLine(18, 104, 36, 116, paint);
                c.drawLine(36, 104, 18, 116, paint);
            } else if (blink) {
                c.drawLine(-38, 112, -18, 112, paint);
                c.drawLine(18, 112, 38, 112, paint);
            } else {
                paint.setStyle(Paint.Style.FILL);
                paint.setColor(Color.rgb(61, 87, 154));
                c.drawOval(new RectF(-40, 100, -18, 128), paint);
                c.drawOval(new RectF(18, 100, 40, 128), paint);
                paint.setColor(Color.WHITE);
                c.drawOval(new RectF(-34, 104, -27, 111), paint);
                c.drawOval(new RectF(24, 104, 31, 111), paint);
            }

            if (state == STATE_HAPPY || state == STATE_SPEAKING) {
                paint.setStyle(Paint.Style.FILL);
                paint.setColor(Color.argb(90, 237, 128, 145));
                c.drawOval(new RectF(-54, 130, -26, 142), paint);
                c.drawOval(new RectF(26, 130, 54, 142), paint);
            }

            paint.setColor(Color.rgb(118, 52, 75));
            paint.setStyle(Paint.Style.STROKE);
            paint.setStrokeWidth(3f);
            if (state == STATE_ERROR) {
                c.drawArc(new RectF(-14, 142, 14, 162), 200, 140, false, paint);
            } else if (state == STATE_THINKING) {
                c.drawLine(-10, 148, 10, 148, paint);
            } else if (state == STATE_SPEAKING) {
                paint.setStyle(Paint.Style.FILL);
                paint.setColor(Color.rgb(154, 55, 89));
                float open = 8 + (frame % 3) * 5;
                c.drawOval(new RectF(-10, 140, 10, 148 + open), paint);
            } else {
                c.drawArc(new RectF(-18, 132, 18, 156), 20, 140, false, paint);
            }
            paint.setStyle(Paint.Style.FILL);
        }

        private void drawBadge(Canvas c, int width) {
            String label = "空闲";
            int color = Color.rgb(80, 120, 170);
            if (state == STATE_THINKING) { label = "思考中"; color = Color.rgb(85, 112, 190); }
            if (state == STATE_SPEAKING) { label = "说话中"; color = Color.rgb(190, 82, 130); }
            if (state == STATE_HAPPY) { label = "完成"; color = Color.rgb(68, 150, 108); }
            if (state == STATE_ERROR) { label = "错误"; color = Color.rgb(190, 76, 76); }

            paint.setStyle(Paint.Style.FILL);
            paint.setColor(color);
            RectF badge = new RectF(width - 102, 12, width - 18, 42);
            c.drawRoundRect(badge, 15, 15, paint);
            paint.setColor(Color.WHITE);
            paint.setTextSize(14 * getResources().getDisplayMetrics().scaledDensity);
            paint.setTextAlign(Paint.Align.CENTER);
            Paint.FontMetrics fm = paint.getFontMetrics();
            float cy = badge.centerY() - (fm.ascent + fm.descent) / 2;
            c.drawText(label, badge.centerX(), cy, paint);
            paint.setTextAlign(Paint.Align.LEFT);
        }
    }
}
