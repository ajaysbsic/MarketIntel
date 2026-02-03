import { Component } from '@angular/core';
import { CommonModule } from '@angular/common';

@Component({
  selector: 'app-about',
  standalone: true,
  imports: [CommonModule],
  template: `
    <div class="about-container">
      <!-- Hero -->
      <section class="about-hero">
        <h1>About Alfanar</h1>
        <p class="subtitle">Real-Time Market Intelligence for Smart Decisions</p>
      </section>

      <!-- Mission Section -->
      <section class="content-section">
        <div class="content-grid">
          <div class="content-box">
            <h2>🎯 Our Mission</h2>
            <p>
              At Alfanar, our mission is to democratize market intelligence by providing real-time, 
              AI-powered insights to investors, traders, and businesses worldwide. We believe that 
              informed decisions drive success, and we're committed to delivering the most accurate 
              and timely market information available.
            </p>
          </div>

          <div class="content-box">
            <h2>💡 Our Vision</h2>
            <p>
              To become the leading platform for market intelligence, trusted by professionals 
              worldwide. We envision a world where every market participant has instant access to 
              comprehensive, unbiased, and actionable market insights powered by cutting-edge AI technology.
            </p>
          </div>
        </div>
      </section>

      <!-- Technology Section -->
      <section class="tech-section">
        <h2>🚀 Our Technology</h2>
        <div class="tech-grid">
          <div class="tech-card">
            <div class="tech-icon">🤖</div>
            <h3>AI & Machine Learning</h3>
            <p>Google Gemini-powered sentiment analysis and natural language processing</p>
          </div>
          <div class="tech-card">
            <div class="tech-icon">📡</div>
            <h3>Real-Time Monitoring</h3>
            <p>Continuous RSS feed monitoring and web crawling across 50+ companies</p>
          </div>
          <div class="tech-card">
            <div class="tech-icon">🔄</div>
            <h3>Live Updates</h3>
            <p>SignalR WebSocket integration for instant notifications and updates</p>
          </div>
          <div class="tech-card">
            <div class="tech-icon">📊</div>
            <h3>Advanced Analytics</h3>
            <p>Financial metrics, trend analysis, and predictive intelligence</p>
          </div>
          <div class="tech-card">
            <div class="tech-icon">🌐</div>
            <h3>Global Coverage</h3>
            <p>Multi-region monitoring across North America, Europe, and Asia-Pacific</p>
          </div>
          <div class="tech-card">
            <div class="tech-icon">⚡</div>
            <h3>High Performance</h3>
            <p>Cloud-native architecture with sub-second response times</p>
          </div>
        </div>
      </section>

      <!-- Key Features -->
      <section class="features-section">
        <h2>✨ Key Features</h2>
        <ul class="features-list">
          <li><strong>Real-Time News Ingestion:</strong> Automatically monitor and ingest news from company RSS feeds</li>
          <li><strong>AI-Powered Analysis:</strong> Automatic sentiment analysis, summarization, and key entity extraction</li>
          <li><strong>Financial Intelligence:</strong> Track financial reports, metrics, EBITDA, and revenue growth</li>
          <li><strong>Smart Alerts:</strong> Critical alerts for market-moving events and opportunities</li>
          <li><strong>Trend Analysis:</strong> Visualize market trends and sentiment changes over time</li>
          <li><strong>Multi-Company Tracking:</strong> Monitor multiple companies across different sectors</li>
          <li><strong>Global Reach:</strong> Support for multiple regions and languages</li>
          <li><strong>Professional Dashboard:</strong> Beautiful, intuitive interface for market analysis</li>
        </ul>
      </section>

      <!-- Stack Section -->
      <section class="stack-section">
        <h2>🛠️ Technology Stack</h2>
        <div class="stack-grid">
          <div class="stack-item">
            <h4>Backend</h4>
            <p>ASP.NET Core 8.0, Entity Framework Core, SQL Server</p>
          </div>
          <div class="stack-item">
            <h4>Frontend</h4>
            <p>Angular 17, TypeScript, RxJS, Material Design</p>
          </div>
          <div class="stack-item">
            <h4>Real-Time</h4>
            <p>SignalR, WebSockets, Message Queues</p>
          </div>
          <div class="stack-item">
            <h4>AI/ML</h4>
            <p>Google Gemini API, NLP, Sentiment Analysis</p>
          </div>
          <div class="stack-item">
            <h4>Data Collection</h4>
            <p>Python, BeautifulSoup, Feedparser, Web Crawling</p>
          </div>
          <div class="stack-item">
            <h4>DevOps</h4>
            <p>Docker, CI/CD, Cloud Deployment</p>
          </div>
        </div>
      </section>

      <!-- Team Section -->
      <section class="team-section">
        <h2>👥 Our Team</h2>
        <p class="team-intro">
          Alfanar is built by a passionate team of data scientists, software engineers, and market analysts 
          dedicated to delivering exceptional market intelligence solutions.
        </p>
        <div class="team-values">
          <div class="value-item">
            <div class="value-icon">🎯</div>
            <h3>Accuracy</h3>
            <p>Precision in every insight we deliver</p>
          </div>
          <div class="value-item">
            <div class="value-icon">⚡</div>
            <h3>Speed</h3>
            <p>Real-time updates and instant alerts</p>
          </div>
          <div class="value-item">
            <div class="value-icon">🤝</div>
            <h3>Reliability</h3>
            <p>99.9% uptime commitment</p>
          </div>
          <div class="value-item">
            <div class="value-icon">🔒</div>
            <h3>Security</h3>
            <p>Enterprise-grade data protection</p>
          </div>
        </div>
      </section>
    </div>
  `,
  styles: [`
    .about-container {
      max-width: 1200px;
      margin: 0 auto;
      padding: 0 2rem;
    }

    /* Hero Section */
    .about-hero {
      text-align: center;
      padding: 4rem 0;
      background: linear-gradient(135deg, #667eea 0%, #764ba2 100%);
      color: white;
      border-radius: 12px;
      margin-bottom: 4rem;
    }

    .about-hero h1 {
      font-size: 3rem;
      font-weight: 700;
      margin-bottom: 1rem;
    }

    .about-hero .subtitle {
      font-size: 1.3rem;
      opacity: 0.9;
    }

    /* Content Section */
    .content-section {
      margin-bottom: 4rem;
    }

    .content-grid {
      display: grid;
      grid-template-columns: 1fr 1fr;
      gap: 2rem;
    }

    .content-box {
      background: #f8f9fa;
      padding: 2rem;
      border-radius: 8px;
      border-left: 4px solid #667eea;
    }

    .content-box h2 {
      font-size: 1.5rem;
      color: #333;
      margin-bottom: 1rem;
      background: linear-gradient(135deg, #667eea 0%, #764ba2 100%);
      -webkit-background-clip: text;
      -webkit-text-fill-color: transparent;
      background-clip: text;
    }

    .content-box p {
      font-size: 1rem;
      line-height: 1.8;
      color: #555;
    }

    /* Tech Section */
    .tech-section {
      margin-bottom: 4rem;
    }

    .tech-section h2 {
      font-size: 2rem;
      color: #333;
      margin-bottom: 2rem;
      text-align: center;
    }

    .tech-grid {
      display: grid;
      grid-template-columns: repeat(auto-fit, minmax(300px, 1fr));
      gap: 2rem;
    }

    .tech-card {
      background: white;
      padding: 2rem;
      border-radius: 8px;
      box-shadow: 0 2px 8px rgba(0, 0, 0, 0.1);
      text-align: center;
      transition: all 0.3s ease;
    }

    .tech-card:hover {
      transform: translateY(-5px);
      box-shadow: 0 8px 20px rgba(102, 126, 234, 0.2);
    }

    .tech-icon {
      font-size: 2.5rem;
      margin-bottom: 1rem;
    }

    .tech-card h3 {
      font-size: 1.2rem;
      color: #333;
      margin-bottom: 0.5rem;
    }

    .tech-card p {
      color: #666;
      font-size: 0.95rem;
      line-height: 1.6;
    }

    /* Features Section */
    .features-section {
      margin-bottom: 4rem;
    }

    .features-section h2 {
      font-size: 2rem;
      color: #333;
      margin-bottom: 2rem;
    }

    .features-list {
      list-style: none;
      padding: 0;
    }

    .features-list li {
      padding: 1rem 0;
      font-size: 1.05rem;
      line-height: 1.6;
      color: #555;
      border-bottom: 1px solid #e0e0e0;
    }

    .features-list li:last-child {
      border-bottom: none;
    }

    /* Stack Section */
    .stack-section {
      margin-bottom: 4rem;
      background: #f8f9fa;
      padding: 3rem 2rem;
      border-radius: 8px;
    }

    .stack-section h2 {
      font-size: 2rem;
      color: #333;
      margin-bottom: 2rem;
      text-align: center;
    }

    .stack-grid {
      display: grid;
      grid-template-columns: repeat(auto-fit, minmax(250px, 1fr));
      gap: 2rem;
    }

    .stack-item {
      background: white;
      padding: 1.5rem;
      border-radius: 8px;
      border-left: 4px solid #667eea;
    }

    .stack-item h4 {
      font-size: 1.1rem;
      color: #333;
      margin-bottom: 0.5rem;
    }

    .stack-item p {
      color: #666;
      font-size: 0.95rem;
    }

    /* Team Section */
    .team-section {
      margin-bottom: 4rem;
      text-align: center;
    }

    .team-section h2 {
      font-size: 2rem;
      color: #333;
      margin-bottom: 1rem;
    }

    .team-intro {
      font-size: 1.1rem;
      color: #555;
      margin-bottom: 2rem;
      max-width: 600px;
      margin-left: auto;
      margin-right: auto;
    }

    .team-values {
      display: grid;
      grid-template-columns: repeat(auto-fit, minmax(200px, 1fr));
      gap: 2rem;
    }

    .value-item {
      background: #f8f9fa;
      padding: 2rem;
      border-radius: 8px;
      border-top: 3px solid #667eea;
    }

    .value-icon {
      font-size: 2rem;
      margin-bottom: 0.5rem;
    }

    .value-item h3 {
      font-size: 1.2rem;
      color: #333;
      margin-bottom: 0.5rem;
    }

    .value-item p {
      color: #666;
      font-size: 0.95rem;
    }

    /* Responsive */
    @media (max-width: 768px) {
      .about-hero h1 {
        font-size: 2rem;
      }

      .about-hero .subtitle {
        font-size: 1rem;
      }

      .content-grid {
        grid-template-columns: 1fr;
      }

      .tech-grid {
        grid-template-columns: 1fr;
      }

      .stack-grid {
        grid-template-columns: 1fr;
      }

      .team-values {
        grid-template-columns: 1fr;
      }
    }
  `]
})
export class AboutComponent {}
