document.addEventListener("DOMContentLoaded", function () {
    const toggle = document.getElementById("chatbot-toggle");
    const widget = document.getElementById("chatbot-widget");
    const closeBtn = document.getElementById("chatbot-close");
    const sendBtn = document.getElementById("chatbot-send");
    const input = document.getElementById("chatbot-input");
    const messages = document.getElementById("chatbot-messages");
    const quickReplies = document.getElementById("chatbot-quick-replies");
    const resetBtn = document.getElementById("chatbot-reset");

    const currentUserId = window.currentChatbotUserId || "guest";
    const STORAGE_KEY = `mentalhealth_chatbot_history_${currentUserId}`;
    const WIDGET_STATE_KEY = `mentalhealth_chatbot_open_${currentUserId}`;

    if (!toggle || !widget || !closeBtn || !sendBtn || !input || !messages) return;

    initializeChatbot();

    toggle.addEventListener("click", function () {
        widget.classList.remove("d-none");
        toggle.style.display = "none";
        localStorage.setItem(WIDGET_STATE_KEY, "open");
        input.focus();
    });

    closeBtn.addEventListener("click", function () {
        widget.classList.add("d-none");
        toggle.style.display = "flex";
        localStorage.setItem(WIDGET_STATE_KEY, "closed");
    });

    sendBtn.addEventListener("click", sendMessage);

    input.addEventListener("keypress", function (e) {
        if (e.key === "Enter") {
            sendMessage();
        }
    });

    if (quickReplies) {
        quickReplies.addEventListener("click", function (e) {
            const btn = e.target.closest(".chatbot-quick-btn");
            if (!btn) return;
            input.value = btn.dataset.message || "";
            sendMessage();
        });
    }

    if (resetBtn) {
        resetBtn.addEventListener("click", async function () {
            await resetConversation();
        });
    }

    async function initializeChatbot() {
        restoreWidgetState();
        await restoreMessagesFromServer();
    }

    async function sendMessage() {
        const text = input.value.trim();
        if (!text) return;

        appendUserMessage(text, true);
        input.value = "";
        showTyping();

        try {
            const response = await fetch("/Chatbot/Ask", {
                method: "POST",
                headers: {
                    "Content-Type": "application/json"
                },
                body: JSON.stringify({ message: text })
            });

            const data = await response.json();
            removeTyping();
            renderBotResponse(data, true);
        } catch (error) {
            removeTyping();
            appendBotText("Hiện tại chatbot đang tạm gián đoạn. Vui lòng thử lại sau.", true);
            console.error(error);
        }
    }

    async function restoreMessagesFromServer() {
        messages.innerHTML = "";

        try {
            const response = await fetch("/Chatbot/GetConversation");
            const history = await response.json();

            if (Array.isArray(history) && history.length > 0) {
                history.forEach(item => {
                    if (item.role === "user") {
                        appendUserMessage(item.text, false);
                    } else if (item.role === "bot") {
                        renderBotResponse({
                            reply: item.reply,
                            type: item.type || "text",
                            items: item.items || null
                        }, false);
                    }
                });

                scrollBottom();
                return;
            }
        } catch (error) {
            console.error("Không tải được lịch sử từ server:", error);
        }

        restoreMessagesFromLocal();
    }

    function restoreMessagesFromLocal() {
        const history = JSON.parse(localStorage.getItem(STORAGE_KEY) || "[]");

        messages.innerHTML = "";

        if (history.length === 0) {
            const wrapper = document.createElement("div");
            wrapper.className = "chatbot-bot-wrapper";
            wrapper.innerHTML = `
                <div class="chatbot-bot-message">
                    Xin chào, mình là chatbot hỗ trợ. Bạn có thể hỏi về chuyên gia, đặt lịch, lịch hẹn hoặc bài viết.
                </div>
            `;
            messages.appendChild(wrapper);
            return;
        }

        history.forEach(item => {
            if (item.role === "user") {
                appendUserMessage(item.text, false);
            } else if (item.role === "bot") {
                renderBotResponse({
                    reply: item.reply,
                    type: item.type,
                    items: item.items
                }, false);
            }
        });

        scrollBottom();
    }

    async function resetConversation() {
        try {
            await fetch("/Chatbot/ResetConversation", {
                method: "POST"
            });
        } catch (error) {
            console.error("Không reset được conversation trên server:", error);
        }

        localStorage.removeItem(STORAGE_KEY);
        localStorage.removeItem(WIDGET_STATE_KEY);

        messages.innerHTML = `
            <div class="chatbot-bot-wrapper">
                <div class="chatbot-bot-message">
                    Xin chào, mình là chatbot hỗ trợ. Bạn có thể hỏi về chuyên gia, đặt lịch, lịch hẹn hoặc bài viết.
                </div>
            </div>
        `;

        widget.classList.add("d-none");
        toggle.style.display = "flex";
    }

    function appendUserMessage(text, save = false) {
        const div = document.createElement("div");
        div.className = "chatbot-user-message";
        div.textContent = text;
        messages.appendChild(div);

        if (save) {
            saveMessage({
                role: "user",
                type: "text",
                text: text
            });
        }

        scrollBottom();
    }

    function appendBotText(text, save = false) {
        const wrapper = document.createElement("div");
        wrapper.className = "chatbot-bot-wrapper";

        const div = document.createElement("div");
        div.className = "chatbot-bot-message";
        div.textContent = text;

        wrapper.appendChild(div);
        messages.appendChild(wrapper);

        if (save) {
            saveMessage({
                role: "bot",
                type: "text",
                reply: text
            });
        }

        scrollBottom();
    }

    function renderBotResponse(data, save = false) {
        const wrapper = document.createElement("div");
        wrapper.className = "chatbot-bot-wrapper";

        const messageDiv = document.createElement("div");
        messageDiv.className = "chatbot-bot-message";
        messageDiv.textContent = data.reply || "Mình chưa có phản hồi phù hợp.";
        wrapper.appendChild(messageDiv);

        if (data.type === "consultants" && Array.isArray(data.items)) {
            const list = document.createElement("div");
            list.className = "chatbot-card-list";

            data.items.forEach(item => {
                const card = document.createElement("div");
                card.className = "chatbot-card";

                card.innerHTML = `
                    <div class="chatbot-card-title">${escapeHtml(item.fullName || "")}</div>
                    <div class="chatbot-card-sub">${escapeHtml(item.specialty || "Chưa cập nhật")}</div>
                    <div class="chatbot-card-meta">${item.experienceYears || 0} năm kinh nghiệm</div>
                    <a class="chatbot-card-btn" href="/Consultants/Details/${item.consultantId}">Xem hồ sơ</a>
                `;

                list.appendChild(card);
            });

            wrapper.appendChild(list);
        }

        if (data.type === "articles" && Array.isArray(data.items)) {
            const list = document.createElement("div");
            list.className = "chatbot-card-list";

            data.items.forEach(item => {
                const url = `/News/Detail/${item.id}`;

                const card = document.createElement("div");
                card.className = "chatbot-card";

                card.innerHTML = `
                    <div class="chatbot-card-title">${escapeHtml(item.title || "")}</div>
                    <div class="chatbot-card-meta">${formatDate(item.createdAt)} • ${escapeHtml(item.sourceType || "News")}</div>
                    <a class="chatbot-card-btn" href="${url}">Xem chi tiết</a>
                `;

                list.appendChild(card);
            });

            wrapper.appendChild(list);
        }

        if (data.type === "appointments" && Array.isArray(data.items)) {
            const list = document.createElement("div");
            list.className = "chatbot-card-list";

            data.items.forEach(item => {
                const card = document.createElement("div");
                card.className = "chatbot-card";

                card.innerHTML = `
                    <div class="chatbot-card-title">${escapeHtml(item.consultantName || "")}</div>
                    <div class="chatbot-card-meta">${formatDateTime(item.appointmentTime)}</div>
                    <div class="chatbot-card-sub">${escapeHtml(item.status || "")}</div>
                    <a class="chatbot-card-btn" href="/Appointments/MyAppointments">Xem lịch hẹn</a>
                `;

                list.appendChild(card);
            });

            wrapper.appendChild(list);
        }

        if (data.type === "actions" && Array.isArray(data.items)) {
            const actionBox = document.createElement("div");
            actionBox.className = "chatbot-actions";

            data.items.forEach(item => {
                const a = document.createElement("a");
                a.className = "chatbot-action-btn";
                a.href = item.url;
                a.textContent = item.label;
                actionBox.appendChild(a);
            });

            wrapper.appendChild(actionBox);
        }

        if (data.type === "quickReplies" && Array.isArray(data.items)) {
            const actionBox = document.createElement("div");
            actionBox.className = "chatbot-actions";

            data.items.forEach(item => {
                const btn = document.createElement("button");
                btn.type = "button";
                btn.className = "chatbot-action-btn chatbot-inline-quick";
                btn.textContent = item;
                btn.addEventListener("click", function () {
                    input.value = item;
                    sendMessage();
                });
                actionBox.appendChild(btn);
            });

            wrapper.appendChild(actionBox);
        }

        messages.appendChild(wrapper);

        if (save) {
            saveMessage({
                role: "bot",
                type: data.type || "text",
                reply: data.reply || "",
                items: data.items || null
            });
        }

        scrollBottom();
    }

    function showTyping() {
        removeTyping();

        const typing = document.createElement("div");
        typing.className = "chatbot-bot-wrapper";
        typing.id = "chatbot-typing";

        typing.innerHTML = `
            <div class="chatbot-bot-message chatbot-typing">
                Bot đang trả lời...
            </div>
        `;

        messages.appendChild(typing);
        scrollBottom();
    }

    function removeTyping() {
        const typing = document.getElementById("chatbot-typing");
        if (typing) typing.remove();
    }

    function saveMessage(messageObj) {
        const history = JSON.parse(localStorage.getItem(STORAGE_KEY) || "[]");
        history.push(messageObj);
        localStorage.setItem(STORAGE_KEY, JSON.stringify(history));
    }

    function restoreWidgetState() {
        const state = localStorage.getItem(WIDGET_STATE_KEY);

        if (state === "open") {
            widget.classList.remove("d-none");
            toggle.style.display = "none";
        } else {
            widget.classList.add("d-none");
            toggle.style.display = "flex";
        }
    }

    function scrollBottom() {
        messages.scrollTop = messages.scrollHeight;
    }

    function formatDate(value) {
        if (!value) return "";
        const d = new Date(value);
        return d.toLocaleDateString("vi-VN");
    }

    function formatDateTime(value) {
        if (!value) return "";
        const d = new Date(value);
        return d.toLocaleString("vi-VN");
    }

    function escapeHtml(text) {
        return String(text)
            .replaceAll("&", "&amp;")
            .replaceAll("<", "&lt;")
            .replaceAll(">", "&gt;")
            .replaceAll('"', "&quot;")
            .replaceAll("'", "&#039;");
    }

    window.clearChatbotHistory = async function () {
        await resetConversation();
    };
});