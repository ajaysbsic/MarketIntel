import { Component, OnInit } from '@angular/core';
import { CommonModule, NgIf } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { ApiService } from '../../shared/services/api.service';

interface ContactForm {
  name: string;
  email: string;
  subject: string;
  message: string;
}

interface CompanyContactInfo {
  company: string;
  location: any;
  contact: any;
  offices: any[];
}

@Component({
  selector: 'app-contact',
  standalone: true,
  imports: [CommonModule, NgIf, FormsModule],
  template: `
    <div class="contact-container">
      <!-- Hero -->
      <section class="contact-hero">
        <h1>Get In Touch</h1>
        <p class="subtitle">We'd love to hear from you. Send us a message!</p>
      </section>

      <!-- Main Content -->
      <div class="contact-content">
        <!-- Contact Form -->
        <section class="contact-form-section">
          <h2>📧 Send us a Message</h2>
          
          <form (ngSubmit)="onSubmit()" class="contact-form">
            <div class="form-group">
              <label for="name">Full Name *</label>
              <input 
                type="text" 
                id="name" 
                [(ngModel)]="formData.name" 
                name="name"
                placeholder="Your full name"
                required
              />
            </div>

            <div class="form-group">
              <label for="email">Email Address *</label>
              <input 
                type="email" 
                id="email" 
                [(ngModel)]="formData.email" 
                name="email"
                placeholder="your.email@company.com"
                required
              />
            </div>

            <div class="form-group">
              <label for="subject">Subject *</label>
              <input 
                type="text" 
                id="subject" 
                [(ngModel)]="formData.subject" 
                name="subject"
                placeholder="How can we help?"
                required
              />
            </div>

            <div class="form-group">
              <label for="message">Message *</label>
              <textarea 
                id="message" 
                [(ngModel)]="formData.message" 
                name="message"
                placeholder="Tell us more about your inquiry..."
                rows="6"
                required
              ></textarea>
            </div>

            <button type="submit" class="submit-btn" [disabled]="isSubmitting">
              {{ isSubmitting ? 'Sending...' : 'Send Message' }}
            </button>

            <div *ngIf="successMessage" class="success-message">
              ✓ {{ successMessage }}
            </div>

            <div *ngIf="errorMessage" class="error-message">
              ✗ {{ errorMessage }}
            </div>
          </form>
        </section>

        <!-- Contact Information -->
        <section class="contact-info-section">
          <h2>📞 Contact Information</h2>
          
          <div class="info-cards">
            <div class="info-card">
              <div class="info-icon">📍</div>
              <h3>Location</h3>
              <p *ngIf="companyInfo && companyInfo.headquarters">
                <strong>{{ companyInfo.headquarters.addressLine1 }}</strong><br>
                {{ companyInfo.headquarters.addressLine2 }}<br>
                {{ companyInfo.headquarters.city }}, {{ companyInfo.headquarters.postalCode }}<br>
                {{ companyInfo.headquarters.country }}
              </p>
            </div>

            <div class="info-card">
              <div class="info-icon">📧</div>
              <h3>Email</h3>
              <p *ngIf="companyInfo && companyInfo.contact">
                <strong>Support:</strong> {{ companyInfo.contact.email.support }}<br>
                <strong>Sales:</strong> {{ companyInfo.contact.email.sales }}
              </p>
            </div>

            <div class="info-card">
              <div class="info-icon">☎️</div>
              <h3>Phone</h3>
              <p *ngIf="companyInfo && companyInfo.contact">
                <strong>Main:</strong> {{ companyInfo.contact.phone.main }}<br>
                <strong>Toll Free:</strong> {{ companyInfo.contact.phone.tollFree }}<br>
                <strong>Available:</strong> {{ companyInfo.contact.phone.availability.days }} {{ companyInfo.contact.phone.availability.hours }} {{ companyInfo.contact.phone.availability.timezone }}
              </p>
            </div>

            <div class="info-card">
              <div class="info-icon">🌐</div>
              <h3>Offices</h3>
              <div *ngIf="companyInfo && companyInfo.offices && companyInfo.offices.length > 0" class="offices-list">
                <div *ngFor="let office of companyInfo.offices" class="office-item">
                  <strong>{{ office.region }}</strong> - {{ office.officeType }}<br>
                  <small *ngIf="office.address">
                    <span *ngIf="office.address.companyName">{{ office.address.companyName }}<br></span>
                    <span *ngIf="office.address.building">{{ office.address.building }}<br></span>
                    <span *ngIf="office.address.floor">{{ office.address.floor }}</span>
                    <span *ngIf="office.address.tower">, {{ office.address.tower }}</span><br>
                    <span *ngIf="office.address.street">{{ office.address.street }}<br></span>
                    <span *ngIf="office.address.area">{{ office.address.area }}</span>
                    <span *ngIf="office.address.district">, {{ office.address.district }}</span><br>
                    <span *ngIf="office.address.city">{{ office.address.city }}</span>
                    <span *ngIf="office.address.postalCode">, {{ office.address.postalCode }}</span><br>
                    <span *ngIf="office.address.country">{{ office.address.country }}</span>
                  </small>
                </div>
              </div>
            </div>
          </div>
        </section>
      </div>

      <!-- FAQ Section -->
      <section class="faq-section">
        <h2>❓ Frequently Asked Questions</h2>
        <div class="faq-grid">
          <div class="faq-item">
            <h3>What is the typical response time?</h3>
            <p>We aim to respond to all inquiries within 24 business hours. Urgent matters are prioritized and may receive responses within 2-4 hours.</p>
          </div>

          <div class="faq-item">
            <h3>Do you offer demo sessions?</h3>
            <p>Absolutely! We offer free demo sessions for prospective clients. Contact our sales team to schedule a session tailored to your needs.</p>
          </div>

          <div class="faq-item">
            <h3>What payment options are available?</h3>
            <p>We accept credit cards, wire transfers, and can discuss custom payment arrangements for enterprise clients.</p>
          </div>

          <div class="faq-item">
            <h3>Is there a free trial?</h3>
            <p>Yes! We offer a 14-day free trial with full access to all features. No credit card required to start.</p>
          </div>

          <div class="faq-item">
            <h3>Do you provide API access?</h3>
            <p>Yes, we provide comprehensive REST APIs for integration with your systems. Documentation and support are included.</p>
          </div>

          <div class="faq-item">
            <h3>What level of support is included?</h3>
            <p>All plans include email support. Premium plans include priority support, phone support, and dedicated account managers.</p>
          </div>
        </div>
      </section>

      <!-- CTA Section -->
      <section class="cta-section">
        <h2>Ready to get started?</h2>
        <p>Join thousands of professionals using Alfanar Market Intelligence for smarter decisions.</p>
        <button class="cta-button">Start Your Free Trial</button>
      </section>
    </div>
  `,
  styles: [`
    .contact-container {
      max-width: 1200px;
      margin: 0 auto;
      padding: 0 2rem;
    }

    /* Hero Section */
    .contact-hero {
      text-align: center;
      padding: 4rem 0;
      background: linear-gradient(135deg, #667eea 0%, #764ba2 100%);
      color: white;
      border-radius: 12px;
      margin-bottom: 4rem;
    }

    .contact-hero h1 {
      font-size: 3rem;
      font-weight: 700;
      margin-bottom: 1rem;
    }

    .contact-hero .subtitle {
      font-size: 1.3rem;
      opacity: 0.9;
    }

    /* Content Layout */
    .contact-content {
      display: grid;
      grid-template-columns: 1.5fr 1fr;
      gap: 3rem;
      margin-bottom: 4rem;
    }

    /* Contact Form Section */
    .contact-form-section h2 {
      font-size: 1.8rem;
      color: #333;
      margin-bottom: 2rem;
    }

    .contact-form {
      background: white;
      padding: 2rem;
      border-radius: 8px;
      box-shadow: 0 2px 8px rgba(0, 0, 0, 0.1);
    }

    .form-group {
      margin-bottom: 1.5rem;
    }

    .form-group label {
      display: block;
      margin-bottom: 0.5rem;
      font-weight: 600;
      color: #333;
      font-size: 0.95rem;
    }

    .form-group input,
    .form-group textarea {
      width: 100%;
      padding: 0.75rem;
      font-size: 1rem;
      border: 1px solid #ddd;
      border-radius: 6px;
      font-family: inherit;
      transition: all 0.3s ease;
    }

    .form-group input:focus,
    .form-group textarea:focus {
      outline: none;
      border-color: #667eea;
      box-shadow: 0 0 0 3px rgba(102, 126, 234, 0.1);
    }

    .submit-btn {
      width: 100%;
      padding: 0.75rem 1.5rem;
      background: linear-gradient(135deg, #667eea 0%, #764ba2 100%);
      color: white;
      border: none;
      border-radius: 6px;
      font-size: 1rem;
      font-weight: 600;
      cursor: pointer;
      transition: all 0.3s ease;
    }

    .submit-btn:hover {
      transform: translateY(-2px);
      box-shadow: 0 4px 12px rgba(102, 126, 234, 0.3);
    }

    .success-message {
      margin-top: 1rem;
      padding: 1rem;
      background: #d4edda;
      color: #155724;
      border-radius: 6px;
      text-align: center;
    }

    .error-message {
      margin-top: 1rem;
      padding: 1rem;
      background: #f8d7da;
      color: #721c24;
      border-radius: 6px;
      text-align: center;
    }

    /* Contact Info Section */
    .contact-info-section h2 {
      font-size: 1.8rem;
      color: #333;
      margin-bottom: 2rem;
    }

    .info-cards {
      display: grid;
      gap: 1.5rem;
    }

    .info-card {
      background: white;
      padding: 1.5rem;
      border-radius: 8px;
      border-left: 4px solid #667eea;
      box-shadow: 0 2px 8px rgba(0, 0, 0, 0.1);
    }

    .info-icon {
      font-size: 2rem;
      margin-bottom: 0.5rem;
    }

    .info-card h3 {
      font-size: 1.1rem;
      color: #333;
      margin-bottom: 0.75rem;
    }

    .info-card p {
      color: #666;
      font-size: 0.95rem;
      line-height: 1.6;
    }

    .offices-list {
      display: flex;
      flex-direction: column;
      gap: 1rem;
    }

    .office-item {
      padding: 1rem;
      background: #f8f9fa;
      border-radius: 6px;
      border-left: 3px solid #667eea;
      color: #666;
      font-size: 0.9rem;
      line-height: 1.5;
    }

    .office-item strong {
      color: #333;
      font-size: 1rem;
      display: block;
      margin-bottom: 0.3rem;
    }

    .office-item small {
      color: #777;
      display: block;
    }

    /* FAQ Section */
    .faq-section {
      margin-bottom: 4rem;
    }

    .faq-section h2 {
      font-size: 2rem;
      color: #333;
      margin-bottom: 2rem;
      text-align: center;
    }

    .faq-grid {
      display: grid;
      grid-template-columns: repeat(auto-fit, minmax(300px, 1fr));
      gap: 2rem;
    }

    .faq-item {
      background: #f8f9fa;
      padding: 1.5rem;
      border-radius: 8px;
      border-top: 3px solid #667eea;
    }

    .faq-item h3 {
      font-size: 1.05rem;
      color: #333;
      margin-bottom: 0.75rem;
    }

    .faq-item p {
      color: #666;
      font-size: 0.95rem;
      line-height: 1.6;
    }

    /* CTA Section */
    .cta-section {
      text-align: center;
      padding: 3rem 2rem;
      background: linear-gradient(135deg, #667eea 0%, #764ba2 100%);
      color: white;
      border-radius: 12px;
      margin-bottom: 4rem;
    }

    .cta-section h2 {
      font-size: 2rem;
      margin-bottom: 1rem;
    }

    .cta-section p {
      font-size: 1.1rem;
      margin-bottom: 2rem;
      opacity: 0.9;
    }

    .cta-button {
      padding: 0.75rem 2rem;
      background: white;
      color: #667eea;
      border: none;
      border-radius: 6px;
      font-size: 1rem;
      font-weight: 600;
      cursor: pointer;
      transition: all 0.3s ease;
    }

    .cta-button:hover {
      transform: translateY(-2px);
      box-shadow: 0 4px 12px rgba(0, 0, 0, 0.2);
    }

    /* Responsive */
    @media (max-width: 1024px) {
      .contact-content {
        grid-template-columns: 1fr;
      }
    }

    @media (max-width: 768px) {
      .contact-hero h1 {
        font-size: 2rem;
      }

      .contact-hero .subtitle {
        font-size: 1rem;
      }

      .contact-form-section h2,
      .contact-info-section h2 {
        font-size: 1.5rem;
      }

      .faq-grid {
        grid-template-columns: 1fr;
      }

      .cta-section h2 {
        font-size: 1.5rem;
      }

      .cta-section p {
        font-size: 1rem;
      }
    }
  `]
})
export class ContactComponent implements OnInit {
  formData: ContactForm = {
    name: '',
    email: '',
    subject: '',
    message: ''
  };

  successMessage = '';
  errorMessage = '';
  isSubmitting = false;
  companyInfo: any = null;

  constructor(private apiService: ApiService) {}

  ngOnInit() {
    // Load company contact information from database
    this.loadCompanyContactInfo();
  }

  loadCompanyContactInfo() {
    this.apiService.getCompanyContact().subscribe({
      next: (data: any) => {
        console.log('Company contact info:', data);
        this.companyInfo = data;
      },
      error: (err: any) => {
        console.error('Error loading company contact info:', err);
        // Keep default values if API fails
      }
    });
  }

  onSubmit() {
    // Validate form
    if (!this.formData.name || !this.formData.email || !this.formData.subject || !this.formData.message) {
      this.errorMessage = 'Please fill in all required fields';
      setTimeout(() => {
        this.errorMessage = '';
      }, 5000);
      return;
    }

    // Email validation
    const emailRegex = /^[^\s@]+@[^\s@]+\.[^\s@]+$/;
    if (!emailRegex.test(this.formData.email)) {
      this.errorMessage = 'Please enter a valid email address';
      setTimeout(() => {
        this.errorMessage = '';
      }, 5000);
      return;
    }

    this.isSubmitting = true;

    // Submit to API
    this.apiService.submitContactForm(this.formData).subscribe({
      next: (response: any) => {
        console.log('Form submitted successfully:', response);
        this.successMessage = 'Thank you for your message! We\'ll get back to you within 24 hours.';
        
        // Reset form
        this.formData = {
          name: '',
          email: '',
          subject: '',
          message: ''
        };

        this.isSubmitting = false;

        // Clear message after 5 seconds
        setTimeout(() => {
          this.successMessage = '';
        }, 5000);
      },
      error: (err: any) => {
        console.error('Error submitting form:', err);
        this.errorMessage = 'Sorry, we encountered an error. Please try again later.';
        this.isSubmitting = false;

        setTimeout(() => {
          this.errorMessage = '';
        }, 5000);
      }
    });
  }
}
