import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { ApiService } from '../../shared/services/api.service';

interface Message {
  id: string;
  content: string;
  sender: 'user' | 'ai';
  timestamp: Date;
  confidence?: number;
  relatedData?: any[];
}

@Component({
  selector: 'app-conversational-ai',
  standalone: true,
  imports: [CommonModule, FormsModule],
  template: `
    <div class="chat-container">
      <h1>AI Market Insights</h1>

      <!-- Chat Area -->
      <div class="chat-area">
        <div class="messages" #messagesContainer>
          <div class="message" *ngFor="let msg of messages" [ngClass]="'message-' + msg.sender">
            <div class="message-header">
              <span class="sender">{{ msg.sender === 'user' ? 'You' : '🤖 AI Assistant' }}</span>
              <span class="time">{{ msg.timestamp | date: 'HH:mm' }}</span>
              <span class="confidence" *ngIf="msg.confidence && msg.sender === 'ai'">
                Confidence: {{ (msg.confidence * 100).toFixed(0) }}%
              </span>
            </div>
            <div class="message-content">{{ msg.content }}</div>
            <div class="related-data" *ngIf="msg.relatedData?.length">
              <strong>Related Data:</strong>
              <ul>
                <li *ngFor="let item of msg.relatedData">{{ item }}</li>
              </ul>
            </div>
          </div>

          <div class="typing-indicator" *ngIf="isLoading">
            <span></span>
            <span></span>
            <span></span>
          </div>
        </div>
      </div>

      <!-- Suggested Queries -->
      <div class="suggested-queries" *ngIf="messages.length === 0">
        <h3>Try asking:</h3>
        <div class="suggestions">
          <button *ngFor="let suggestion of suggestedQueries" (click)="useSuggestion(suggestion)" class="suggestion-btn">
            {{ suggestion }}
          </button>
        </div>
      </div>

      <!-- Input Area -->
      <div class="input-area">
        <form (ngSubmit)="sendMessage()" class="input-form">
          <input
            type="text"
            [(ngModel)]="userInput"
            name="message"
            placeholder="Ask about market sentiment, trends, alerts..."
            [disabled]="isLoading"
            class="message-input"
          />
          <button type="submit" [disabled]="!userInput || isLoading" class="btn-send">
            {{ isLoading ? 'Sending...' : 'Send' }}
          </button>
        </form>
        <button (click)="clearChat()" class="btn-clear">Clear Chat</button>
      </div>
    </div>
  `,
  styles: [`
    .chat-container {
      display: flex;
      flex-direction: column;
      height: calc(100vh - 300px);
      gap: 1rem;
    }

    h1 {
      margin: 0;
    }

    .chat-area {
      flex: 1;
      background: var(--bg-secondary);
      border: 1px solid var(--border-color);
      border-radius: 8px;
      overflow-y: auto;
      padding: 1.5rem;
      display: flex;
      flex-direction: column;
    }

    .messages {
      display: flex;
      flex-direction: column;
      gap: 1rem;
    }

    .message {
      display: flex;
      flex-direction: column;
      gap: 0.5rem;
      animation: slideIn 0.3s ease-out;
    }

    @keyframes slideIn {
      from {
        opacity: 0;
        transform: translateY(10px);
      }
      to {
        opacity: 1;
        transform: translateY(0);
      }
    }

    .message-user {
      align-items: flex-end;
    }

    .message-ai {
      align-items: flex-start;
    }

    .message-header {
      display: flex;
      gap: 1rem;
      font-size: 0.85rem;
      color: var(--text-secondary);
    }

    .sender {
      font-weight: 500;
    }

    .confidence {
      color: var(--success);
    }

    .message-content {
      max-width: 70%;
      padding: 1rem;
      border-radius: 8px;
      line-height: 1.5;
    }

    .message-user .message-content {
      background: linear-gradient(135deg, var(--primary-color), var(--secondary-color));
      color: white;
      border-radius: 12px 0 12px 12px;
    }

    .message-ai .message-content {
      background: var(--bg-primary);
      border: 1px solid var(--border-color);
      border-radius: 0 12px 12px 12px;
    }

    .related-data {
      max-width: 70%;
      padding: 0.75rem;
      background: rgba(52, 152, 219, 0.1);
      border-left: 3px solid var(--info);
      border-radius: 4px;
      font-size: 0.9rem;
    }

    .related-data ul {
      margin: 0.5rem 0 0 1rem;
      padding-left: 1rem;
    }

    .related-data li {
      margin: 0.25rem 0;
    }

    .suggested-queries {
      text-align: center;
      padding: 2rem;
      background: var(--bg-secondary);
      border-radius: 8px;
    }

    .suggested-queries h3 {
      margin-bottom: 1rem;
    }

    .suggestions {
      display: flex;
      flex-wrap: wrap;
      gap: 0.75rem;
      justify-content: center;
    }

    .suggestion-btn {
      background: var(--bg-primary);
      border: 1px solid var(--border-color);
      padding: 0.75rem 1rem;
      border-radius: 6px;
      cursor: pointer;
      transition: all 0.3s ease;
      color: var(--text-primary);
    }

    .suggestion-btn:hover {
      background: var(--primary-color);
      color: white;
      border-color: var(--primary-color);
    }

    .input-area {
      display: flex;
      gap: 1rem;
      padding: 1rem;
      background: var(--bg-secondary);
      border: 1px solid var(--border-color);
      border-radius: 8px;
    }

    .input-form {
      flex: 1;
      display: flex;
      gap: 0.5rem;
    }

    .message-input {
      flex: 1;
      padding: 0.75rem 1rem;
      border: 1px solid var(--border-color);
      border-radius: 6px;
      font-size: 1rem;
    }

    .btn-send {
      background: linear-gradient(135deg, var(--primary-color), var(--secondary-color));
      color: white;
      border: none;
      padding: 0.75rem 1.5rem;
      border-radius: 6px;
      cursor: pointer;
      font-weight: 500;
      transition: opacity 0.3s ease;
    }

    .btn-send:hover:not(:disabled) {
      opacity: 0.9;
    }

    .btn-send:disabled {
      opacity: 0.5;
      cursor: not-allowed;
    }

    .btn-clear {
      background: var(--bg-primary);
      border: 1px solid var(--border-color);
      padding: 0.75rem 1rem;
      border-radius: 6px;
      cursor: pointer;
      color: var(--text-primary);
    }

    .btn-clear:hover {
      background: var(--border-color);
    }

    .typing-indicator {
      display: flex;
      gap: 0.25rem;
      padding: 1rem;
    }

    .typing-indicator span {
      width: 8px;
      height: 8px;
      background: var(--text-secondary);
      border-radius: 50%;
      animation: bounce 1.4s infinite;
    }

    .typing-indicator span:nth-child(2) {
      animation-delay: 0.2s;
    }

    .typing-indicator span:nth-child(3) {
      animation-delay: 0.4s;
    }

    @keyframes bounce {
      0%,
      80%,
      100% {
        opacity: 0.5;
        transform: translateY(0);
      }
      40% {
        opacity: 1;
        transform: translateY(-10px);
      }
    }

    @media (max-width: 768px) {
      .message-content,
      .related-data {
        max-width: 100%;
      }

      .suggestions {
        flex-direction: column;
      }

      .input-area {
        flex-direction: column;
      }
    }
  `],
})
export class ConversationalAiComponent implements OnInit {
  messages: Message[] = [];
  userInput = '';
  isLoading = false;
  suggestedQueries = [
    'What is the current market sentiment?',
    'Which sectors are performing best?',
    'Show me recent alerts',
    'What are the top keywords today?',
    'Analyze today\' news trends',
    'What companies are trending?',
  ];

  constructor(private apiService: ApiService) {}

  ngOnInit(): void {
    // Initialize with greeting
    this.messages.push({
      id: '0',
      content: 'Hello! I\'m your AI market intelligence assistant. Ask me about market trends, sentiment analysis, or any recent news and alerts.',
      sender: 'ai',
      timestamp: new Date(),
      confidence: 1,
    });
  }

  sendMessage(): void {
    if (!this.userInput.trim()) return;

    // Add user message
    const userMessage: Message = {
      id: Date.now().toString(),
      content: this.userInput,
      sender: 'user',
      timestamp: new Date(),
    };
    this.messages.push(userMessage);
    const query = this.userInput;
    this.userInput = '';
    this.isLoading = true;

    // Query AI
    this.apiService.queryConversationalAI(query).subscribe({
      next: (response) => {
        const aiMessage: Message = {
          id: (Date.now() + 1).toString(),
          content: response.response || 'I could not generate a response. Please try again.',
          sender: 'ai',
          timestamp: new Date(),
          confidence: response.confidence || 0.85,
          relatedData: response.relatedData?.map((item: any) => item.title || item.name),
        };
        this.messages.push(aiMessage);
        this.isLoading = false;
        this.scrollToBottom();
      },
      error: (err) => {
        console.error('Failed to get AI response:', err);
        const errorMessage: Message = {
          id: (Date.now() + 1).toString(),
          content: 'Sorry, I encountered an error processing your request. Please try again.',
          sender: 'ai',
          timestamp: new Date(),
        };
        this.messages.push(errorMessage);
        this.isLoading = false;
      },
    });
  }

  useSuggestion(suggestion: string): void {
    this.userInput = suggestion;
    this.sendMessage();
  }

  clearChat(): void {
    this.messages = [
      {
        id: '0',
        content: 'Chat cleared. How can I help you today?',
        sender: 'ai',
        timestamp: new Date(),
        confidence: 1,
      },
    ];
  }

  scrollToBottom(): void {
    setTimeout(() => {
      const messagesContainer = document.querySelector('.messages');
      if (messagesContainer) {
        messagesContainer.scrollTop = messagesContainer.scrollHeight;
      }
    }, 0);
  }
}
