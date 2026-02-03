import { Injectable } from '@angular/core';
import { BehaviorSubject, Observable } from 'rxjs';

export interface Theme {
  name: string;
  colors: {
    primary: string;
    secondary: string;
    success: string;
    danger: string;
    warning: string;
    info: string;
    bgPrimary: string;
    bgSecondary: string;
    textPrimary: string;
    textSecondary: string;
    borderColor: string;
  };
}

@Injectable({
  providedIn: 'root',
})
export class ThemeService {
  private lightTheme: Theme = {
    name: 'light',
    colors: {
      primary: '#1f47ba',
      secondary: '#0d3a7a',
      success: '#27ae60',
      danger: '#e74c3c',
      warning: '#f39c12',
      info: '#3498db',
      bgPrimary: '#f5f5f5',
      bgSecondary: '#f9f9f9',
      textPrimary: '#333333',
      textSecondary: '#666666',
      borderColor: '#dddddd',
    },
  };

  private darkTheme: Theme = {
    name: 'dark',
    colors: {
      primary: '#5b7cff',
      secondary: '#4a68e6',
      success: '#3fb950',
      danger: '#f85149',
      warning: '#fb8500',
      info: '#58a6ff',
      bgPrimary: '#1a1a1a',
      bgSecondary: '#2a2a2a',
      textPrimary: '#f0f0f0',
      textSecondary: '#b0b0b0',
      borderColor: '#444444',
    },
  };

  private currentTheme$ = new BehaviorSubject<Theme>(this.lightTheme);
  isDarkMode$ = new BehaviorSubject<boolean>(false);

  constructor() {
    this.initializeTheme();
  }

  private initializeTheme(): void {
    const savedTheme = localStorage.getItem('theme');
    const prefersDark = window.matchMedia('(prefers-color-scheme: dark)').matches;
    
    const isDark = savedTheme ? savedTheme === 'dark' : prefersDark;
    this.setTheme(isDark ? 'dark' : 'light');
  }

  setTheme(themeName: 'light' | 'dark'): void {
    const theme = themeName === 'dark' ? this.darkTheme : this.lightTheme;
    this.currentTheme$.next(theme);
    this.isDarkMode$.next(themeName === 'dark');
    this.applyThemeToDOM(theme, themeName);
    localStorage.setItem('theme', themeName);
  }

  toggleTheme(): void {
    const isDark = this.isDarkMode$.value;
    this.setTheme(isDark ? 'light' : 'dark');
  }

  getTheme(): Observable<Theme> {
    return this.currentTheme$.asObservable();
  }

  private applyThemeToDOM(theme: Theme, themeName: string): void {
    const root = document.documentElement;
    
    Object.entries(theme.colors).forEach(([key, value]) => {
      const cssVarName = `--${key.replace(/([A-Z])/g, '-$1').toLowerCase().replace(/^-/, '')}`;
      root.style.setProperty(cssVarName, value);
    });

    if (themeName === 'dark') {
      document.body.classList.add('dark-theme');
    } else {
      document.body.classList.remove('dark-theme');
    }
  }
}
