import { Component, OnInit, ViewChild, ElementRef } from '@angular/core';
import { CommonModule, NgIf, NgFor } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { HttpClient } from '@angular/common/http';
import { environment } from '../../../environments/environment';

interface ChatMessage {
  id: string;
  role: 'user' | 'assistant';
  content: string;
  timestamp: string;
  aiResponse?: AiResponse;
}

interface AiResponse {
  answer: string;
  citations: Citation[];
  confidence: number;
  relatedQueries: string[];
  executionTimeMs: number;
}

interface Citation {
  sourceId: string;
  sourceType: string;
  title: string;
  publishedDate: string;
  url: string;
}

@Component({
  selector: 'app-ai-chat',
  standalone: true,
  imports: [CommonModule, NgIf, NgFor, FormsModule],
  template: `
    <div class="chat-container">
      <!-- Header -->
      <div class="chat-header">
        <h2>Market Intelligence AI Chat</h2>
        <p class="subtitle">Powered by RAG (Retrieval Augmented Generation)</p>
      </div>

      <!-- Messages Display -->
      <div class="chat-messages" #messagesContainer>
        <div *ngFor="let message of chatHistory" 
             [ngClass]="{'message-user': message.role === 'user', 'message-assistant': message.role === 'assistant'}"
             class="chat-message">
          
          <!-- User Message -->
          <div *ngIf="message.role === 'user'" class="message-content">
            <p>{{ message.content }}</p>
          </div>

          <!-- Assistant Message with Full Details -->
          <div *ngIf="message.role === 'assistant'" class="message-content assistant">
            <!-- Main Answer -->
            <div class="answer">
              <p>{{ message.aiResponse?.answer }}</p>
            </div>

            <!-- Confidence Level -->
            <div class="confidence-indicator" 
                 [ngClass]="{'high': message.aiResponse?.confidence > 0.8, 'medium': message.aiResponse?.confidence > 0.5, 'low': message.aiResponse?.confidence <= 0.5}">
              Confidence: {{ (message.aiResponse?.confidence * 100).toFixed(0) }}%
            </div>

            <!-- Citations -->
            <div *ngIf="message.aiResponse?.citations && message.aiResponse.citations.length > 0" class="citations">
              <h4>📚 Sources Used:</h4>
              <div *ngFor="let citation of message.aiResponse.citations" class="citation">
                <a [href]="citation.url" target="_blank">
                  <strong>{{ citation.title }}</strong>
                </a>
                <span class="source-type">[{{ citation.sourceType }}]</span>
                <span class="date">{{ citation.publishedDate | date:'short' }}</span>
              </div>
            </div>

            <!-- Related Queries -->
            <div *ngIf="message.aiResponse?.relatedQueries && message.aiResponse.relatedQueries.length > 0" class="related-queries">
              <h4>💡 Related Queries:</h4>
              <div class="query-buttons">
                <button *ngFor="let query of message.aiResponse.relatedQueries" 
                        (click)="onRelatedQueryClick(query)"
                        class="query-button">
                  {{ query }}
                </button>
              </div>
            </div>

            <!-- Execution Time -->
            <div class="execution-time">
              Response time: {{ message.aiResponse?.executionTimeMs }}ms
            </div>
          </div>
        </div>

        <!-- Loading Indicator -->
        <div *ngIf="isLoading" class="message-assistant chat-message">
          <div class="message-content assistant">
            <div class="loading-spinner">
              <span>Analyzing your query with RAG...</span>
              <div class="spinner"></div>
            </div>
          </div>
        </div>
      </div>

      <!-- Input Area -->
      <div class="chat-input-area">
        <div class="input-container">
          <input 
            [(ngModel)]="userInput" 
            (keyup.enter)="onSendMessage()"
            [disabled]="isLoading"
            placeholder="Ask about market trends, companies, or financial data..."
            class="chat-input"
          />
          <button 
            (click)="onSendMessage()"
            [disabled]="!userInput.trim() || isLoading"
            class="send-button">
            {{ isLoading ? 'Sending...' : 'Send' }}
          </button>
        </div>

        <!-- Suggested Queries -->
        <div class="suggestions">
          <p>Quick queries:</p>
          <button *ngFor="let suggestion of suggestedQueries"
                  (click)="userInput = suggestion; onSendMessage()"
                  class="suggestion-button">
            {{ suggestion }}
          </button>
        </div>
      </div>
    </div>
  `,
  styles: [`
    .chat-container {
      display: flex;
      flex-direction: column;
      height: 100vh;
      background: linear-gradient(135deg, #667eea 0%, #764ba2 100%);
      font-family: 'Segoe UI', Tahoma, Geneva, Verdana, sans-serif;
    }

    .chat-header {
      background: rgba(0, 0, 0, 0.2);
      color: white;
      padding: 1.5rem 2rem;
      text-align: center;
      box-shadow: 0 2px 10px rgba(0, 0, 0, 0.1);
    }

    .chat-header h2 {
      margin: 0;
      font-size: 1.8rem;
      font-weight: 700;
    }

    .subtitle {
      margin: 0.5rem 0 0 0;
      opacity: 0.9;
      font-size: 0.9rem;
    }

    .chat-messages {
      flex: 1;
      overflow-y: auto;
      padding: 2rem;
      background: white;
      display: flex;
      flex-direction: column;
      gap: 1.5rem;
    }

    .chat-message {
      display: flex;
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
      justify-content: flex-end;
    }

    .message-user .message-content {
      background: #667eea;
      color: white;
      border-radius: 18px 4px 18px 18px;
      max-width: 70%;
    }

    .message-assistant {
      justify-content: flex-start;
    }

    .message-assistant .message-content {
      background: #f0f0f0;
      color: #333;
      border-radius: 4px 18px 18px 18px;
      max-width: 85%;
    }

    .message-content {
      padding: 1rem 1.5rem;
    }

    .message-content p {
      margin: 0;
      line-height: 1.6;
    }

    .answer {
      margin-bottom: 1rem;
      font-size: 0.95rem;
      line-height: 1.7;
    }

    .confidence-indicator {
      display: inline-block;
      padding: 0.4rem 0.8rem;
      border-radius: 4px;
      font-size: 0.85rem;
      font-weight: 600;
      margin: 0.5rem 0;
    }

    .confidence-indicator.high {
      background: #d4edda;
      color: #155724;
    }

    .confidence-indicator.medium {
      background: #fff3cd;
      color: #856404;
    }

    .confidence-indicator.low {
      background: #f8d7da;
      color: #721c24;
    }

    .citations {
      margin-top: 1rem;
      padding: 1rem;
      background: rgba(0, 0, 0, 0.05);
      border-radius: 8px;
      border-left: 4px solid #667eea;
    }

    .citations h4 {
      margin: 0 0 0.8rem 0;
      font-size: 0.9rem;
      color: #333;
    }

    .citation {
      display: flex;
      align-items: center;
      gap: 0.8rem;
      margin-bottom: 0.6rem;
      font-size: 0.85rem;
    }

    .citation a {
      color: #667eea;
      text-decoration: none;
      font-weight: 500;
    }

    .citation a:hover {
      text-decoration: underline;
    }

    .source-type {
      background: #667eea;
      color: white;
      padding: 0.2rem 0.6rem;
      border-radius: 12px;
      font-size: 0.75rem;
    }

    .date {
      color: #999;
      font-size: 0.8rem;
    }

    .related-queries {
      margin-top: 1rem;
      padding: 1rem;
      background: rgba(102, 126, 234, 0.1);
      border-radius: 8px;
    }

    .related-queries h4 {
      margin: 0 0 0.8rem 0;
      font-size: 0.9rem;
      color: #333;
    }

    .query-buttons {
      display: flex;
      flex-wrap: wrap;
      gap: 0.6rem;
    }

    .query-button {
      background: white;
      border: 1px solid #667eea;
      color: #667eea;
      padding: 0.4rem 0.8rem;
      border-radius: 16px;
      font-size: 0.85rem;
      cursor: pointer;
      transition: all 0.2s;
    }

    .query-button:hover {
      background: #667eea;
      color: white;
    }

    .execution-time {
      margin-top: 0.8rem;
      font-size: 0.8rem;
      color: #999;
      text-align: right;
    }

    .loading-spinner {
      display: flex;
      align-items: center;
      gap: 1rem;
    }

    .spinner {
      width: 20px;
      height: 20px;
      border: 3px solid #f3f3f3;
      border-top: 3px solid #667eea;
      border-radius: 50%;
      animation: spin 1s linear infinite;
    }

    @keyframes spin {
      0% { transform: rotate(0deg); }
      100% { transform: rotate(360deg); }
    }

    .chat-input-area {
      background: white;
      padding: 1.5rem 2rem;
      border-top: 1px solid #e0e0e0;
    }

    .input-container {
      display: flex;
      gap: 1rem;
      margin-bottom: 1rem;
    }

    .chat-input {
      flex: 1;
      padding: 0.8rem 1rem;
      border: 1px solid #ddd;
      border-radius: 8px;
      font-size: 0.95rem;
      transition: border-color 0.2s;
    }

    .chat-input:focus {
      outline: none;
      border-color: #667eea;
      box-shadow: 0 0 0 3px rgba(102, 126, 234, 0.1);
    }

    .send-button {
      padding: 0.8rem 2rem;
      background: #667eea;
      color: white;
      border: none;
      border-radius: 8px;
      font-weight: 600;
      cursor: pointer;
      transition: background 0.2s;
    }

    .send-button:hover:not(:disabled) {
      background: #5568d3;
    }

    .send-button:disabled {
      opacity: 0.5;
      cursor: not-allowed;
    }

    .suggestions {
      font-size: 0.85rem;
      color: #666;
    }

    .suggestions p {
      margin: 0 0 0.8rem 0;
    }

    .suggestion-button {
      display: inline-block;
      margin-right: 0.8rem;
      margin-bottom: 0.6rem;
      padding: 0.5rem 1rem;
      background: #f0f0f0;
      border: 1px solid #ddd;
      border-radius: 16px;
      font-size: 0.85rem;
      cursor: pointer;
      transition: all 0.2s;
    }

    .suggestion-button:hover {
      background: #667eea;
      color: white;
      border-color: #667eea;
    }

    @media (max-width: 768px) {
      .message-user .message-content,
      .message-assistant .message-content {
        max-width: 95%;
      }

      .chat-header h2 {
        font-size: 1.4rem;
      }
    }
  `]
})
export class AiChatComponent implements OnInit {
  @ViewChild('messagesContainer') messagesContainer: ElementRef | undefined;

  userInput = '';
  isLoading = false;
  chatHistory: ChatMessage[] = [];
  suggestedQueries = [
    'What are the latest technology trends?',
    'Analyze Samsung\'s market position',
    'What financial risks should I monitor?',
    'Generate a market sentiment report'
  ];

  constructor(private http: HttpClient) {}

  ngOnInit() {
    // Load any saved chat history
    this.loadChatHistory();
  }

  onSendMessage() {
    if (!this.userInput.trim() || this.isLoading) return;

    const userMessage: ChatMessage = {
      id: this.generateId(),
      role: 'user',
      content: this.userInput,
      timestamp: new Date().toISOString()
    };

    this.chatHistory.push(userMessage);
    const query = this.userInput;
    this.userInput = '';
    this.isLoading = true;

    this.http.post<any>(`${environment.apiUrl}/api/aichat/query`, {
      message: query,
      conversationHistory: this.chatHistory
    }).subscribe({
      next: (response: AiResponse) => {
        const assistantMessage: ChatMessage = {
          id: this.generateId(),
          role: 'assistant',
          content: response.answer,
          timestamp: new Date().toISOString(),
          aiResponse: response
        };

        this.chatHistory.push(assistantMessage);
        this.isLoading = false;
        this.saveChatHistory();
        this.scrollToBottom();
      },
      error: (error) => {
        console.error('Error:', error);
        const errorMessage: ChatMessage = {
          id: this.generateId(),
          role: 'assistant',
          content: 'Sorry, I encountered an error processing your request. Please try again.',
          timestamp: new Date().toISOString()
        };
        this.chatHistory.push(errorMessage);
        this.isLoading = false;
      }
    });
  }

  onRelatedQueryClick(query: string) {
    this.userInput = query;
    setTimeout(() => this.onSendMessage(), 100);
  }

  private scrollToBottom() {
    setTimeout(() => {
      if (this.messagesContainer) {
        const element = this.messagesContainer.nativeElement;
        element.scrollTop = element.scrollHeight;
      }
    }, 0);
  }

  private generateId() {
    return `${Date.now()}-${Math.random()}`;
  }

  private saveChatHistory() {
    localStorage.setItem('chatHistory', JSON.stringify(this.chatHistory));
  }

  private loadChatHistory() {
    const saved = localStorage.getItem('chatHistory');
    if (saved) {
      this.chatHistory = JSON.parse(saved);
    }
  }
}
