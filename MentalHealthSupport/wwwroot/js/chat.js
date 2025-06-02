if (typeof window.signalRChatInstance === 'undefined') {
    class SignalRChat {
        constructor() {
            this.connection = null;
            this.chatSessionId = null;
            this.userId = null;
            this.isActive = false;
        }

        init() {
            if (typeof signalR === 'undefined') {
                console.error("SignalR library not loaded.");
                return;
            }
            const userIdElement = document.querySelector('input[name="userId"]');
            this.userId = userIdElement ? parseInt(userIdElement.value) : null;
            if (!this.userId || isNaN(this.userId)) {
                console.error("UserId not found in DOM or invalid.");
                return;
            }
            console.log("UserId from DOM:", this.userId);
            this.initSignalR();
        }

        initSignalR() {
            this.connection = new signalR.HubConnectionBuilder()
                .withUrl("/chatHub")
                .build();

            this.connection.on("ReceiveMessage", (chatSessionId, senderId, message, timestamp) => {
                this.addMessage(message, senderId === this.userId ? 'user' : 'expert', timestamp);
            });

            this.connection.on("ChatStarted", (chatSessionId) => {
                console.log("Chat started with session ID:", chatSessionId);
                this.chatSessionId = chatSessionId;
                this.updateStatus(true);
                this.addSystemMessage("Đã kết nối với chuyên gia!");
            });

            this.connection.on("ChatEnded", () => {
                console.log("Chat ended");
                this.updateStatus(false);
                this.addSystemMessage("Cuộc trò chuyện đã kết thúc.");
                setTimeout(() => {
                    $('#chatModal').modal('hide');
                }, 2000);
            });

            this.connection.on("Error", (message) => {
                console.error("Server error:", message);
                this.addSystemMessage(message, "error");
            });

            this.connection.start()
                .then(() => {
                    console.log("SignalR Connected");
                })
                .catch(err => {
                    console.error("SignalR Connection Error:", err);
                });
        }

        bindEvents() {
            const startChatBtn = document.getElementById("startChat");
            const endChatBtn = document.getElementById("endChat");
            const sendBtn = document.getElementById("sendButton");
            const messageInput = document.getElementById("messageInput");

            if (!startChatBtn || !endChatBtn || !sendBtn || !messageInput) {
                console.error("DOM elements not found:", { startChatBtn, endChatBtn, sendBtn, messageInput });
                this.addSystemMessage("Một số thành phần chat không tải được. Vui lòng thử lại.", "error");
                return;
            }

            startChatBtn.addEventListener("click", async () => {
                console.log("Start chat button clicked");
                await this.startChat();
            });
            endChatBtn.addEventListener("click", () => this.endChat());
            sendBtn.addEventListener("click", () => this.sendMessage());
            messageInput.addEventListener("keypress", (e) => {
                if (e.which === 13 || e.key === 'Enter') this.sendMessage();
            });
        }

        async startChat() {
            try {
                this.addSystemMessage("Đang tìm chuyên gia...", "info");
                console.log("Sending request to /Account/AssignConsultant with userId:", this.userId);
                const response = await fetch('/Account/AssignConsultant', {
                    method: 'POST',
                    headers: { 'Content-Type': 'application/json' },
                    body: JSON.stringify({ userId: this.userId })
                });

                if (!response.ok) {
                    const text = await response.text();
                    console.error("AssignConsultant failed:", text);
                    throw new Error(`HTTP error! status: ${response.status}, message: ${text}`);
                }

                const data = await response.json();
                console.log("AssignConsultant response:", data);
                const consultantId = data.ConsultantId || data.consultantId;

                if (!consultantId) {
                    throw new Error("ConsultantId not found in response");
                }

                console.log("Starting chat with userId:", this.userId, "and consultantId:", consultantId);
                await this.connection.invoke("StartChat", this.userId, consultantId);
            } catch (err) {
                console.error("Start chat error:", err.message);
                this.addSystemMessage("Không thể bắt đầu chat: " + err.message, "error");
            }
        }

        endChat() {
            if (!this.chatSessionId) return;
            try {
                this.connection.invoke("EndChat", this.chatSessionId);
            } catch (err) {
                console.error("End chat error:", err);
            }
        }

        sendMessage() {
            const messageInput = document.getElementById("messageInput");
            const message = messageInput.value.trim();
            if (!message || !this.chatSessionId) return;

            try {
                this.connection.invoke("SendMessage", this.chatSessionId, this.userId, message);
                messageInput.value = "";
            } catch (err) {
                console.error("Send message error:", err);
                this.addSystemMessage("Không thể gửi tin nhắn: " + err.message, "error");
            }
        }

        updateStatus(isOnline) {
            this.isActive = isOnline;
            const startBtn = document.getElementById('startChat');
            const endBtn = document.getElementById('endChat');
            const sendBtn = document.getElementById('sendButton');
            const messageInput = document.getElementById('messageInput');
            const statusBadge = document.getElementById('statusBadge');

            if (!startBtn || !endBtn || !sendBtn || !messageInput || !statusBadge) {
                console.error("DOM elements for updateStatus not found:", { startBtn, endBtn, sendBtn, messageInput, statusBadge });
                return;
            }

            if (isOnline) {
                startBtn.disabled = true;
                endBtn.disabled = false;
                sendBtn.disabled = false;
                messageInput.disabled = false;
                statusBadge.className = 'badge bg-success status-badge';
                statusBadge.innerHTML = '<span class="status-dot online me-1"></span>Online';
                messageInput.focus();
            } else {
                startBtn.disabled = false;
                endBtn.disabled = true;
                sendBtn.disabled = true;
                messageInput.disabled = true;
                statusBadge.className = 'badge bg-danger status-badge';
                statusBadge.innerHTML = '<span class="status-dot offline me-1"></span>Offline';
                this.chatSessionId = null;
            }
        }

        addMessage(text, sender, timestamp) {
            const chatBox = document.getElementById('chatBox');
            const emptyState = document.getElementById('emptyState');
            if (!chatBox) {
                console.error("ChatBox not found in DOM.");
                return;
            }
            if (emptyState) emptyState.remove();

            const messageDiv = document.createElement('div');
            messageDiv.className = `message ${sender}`;
            const time = new Date(timestamp).toLocaleTimeString('vi-VN', { hour: '2-digit', minute: '2-digit' });
            messageDiv.innerHTML = `
                <div class="message-bubble-${sender}">${text}</div>
                <small class="text-muted mt-1">${time}</small>
            `;
            chatBox.appendChild(messageDiv);
            chatBox.scrollTop = chatBox.scrollHeight;
        }

        addSystemMessage(text, type = "info") {
            const chatBox = document.getElementById('chatBox');
            if (!chatBox) {
                console.error("ChatBox not found in DOM.");
                return;
            }
            const emptyState = document.getElementById('emptyState');
            if (emptyState) emptyState.remove();

            const messageDiv = document.createElement('div');
            messageDiv.className = 'mb-3 text-center';
            const badgeClass = type === "error" ? "bg-danger" : type === "info" ? "bg-info" : "bg-secondary";
            messageDiv.innerHTML = `
                <span class="badge ${badgeClass}"><i class="fas fa-info-circle me-1"></i>${text}</span>
            `;
            chatBox.appendChild(messageDiv);
            chatBox.scrollTop = chatBox.scrollHeight;
        }
    }

    // Trì hoãn khởi tạo để đảm bảo modal đã hiển thị
    $('#chatModal').on('shown.bs.modal', function () {
        console.log("Chat modal shown, initializing SignalRChat...");
        const modalBody = document.querySelector('#chatModal .modal-body');
        if (!modalBody) {
            console.error("Modal body not found in DOM.");
            return;
        }
        const startChatBtn = document.getElementById("startChat");
        if (!startChatBtn) {
            console.error("startChat button not found in DOM after modal shown.");
            return;
        }

        if (!window.signalRChatInstance) {
            window.signalRChatInstance = new SignalRChat();
            window.signalRChatInstance.init();
            window.signalRChatInstance.bindEvents();
        } else {
            window.signalRChatInstance.bindEvents();
        }
        $('#chatModal').removeAttr('aria-hidden');
    });

    // Gắn sự kiện mở modal sau khi DOM sẵn sàng
    document.addEventListener('DOMContentLoaded', () => {
        const openChatModalBtn = document.getElementById("openChatModal");
        if (openChatModalBtn) {
            openChatModalBtn.addEventListener("click", () => {
                console.log("Opening chat modal...");
                $('#chatModal').modal('show');
            });
        } else {
            console.error("openChatModal button not found in DOM after DOMContentLoaded.");
        }
    });
}