import { MarkdownPipe } from './markdown.pipe';
import { DomSanitizer, SafeHtml } from '@angular/platform-browser';
import { TestBed } from '@angular/core/testing';
import { BrowserModule } from '@angular/platform-browser';

describe('MarkdownPipe', () => {
  let pipe: MarkdownPipe;
  let sanitizer: DomSanitizer;

  beforeEach(() => {
    TestBed.configureTestingModule({
      imports: [BrowserModule]
    });
    sanitizer = TestBed.inject(DomSanitizer);
    pipe = new MarkdownPipe(sanitizer);
  });

  it('should create the pipe', () => {
    expect(pipe).toBeTruthy();
  });

  it('should return empty string for null', () => {
    expect(pipe.transform(null)).toBe('');
  });

  it('should return empty string for undefined', () => {
    expect(pipe.transform(undefined)).toBe('');
  });

  it('should return empty string for empty string', () => {
    expect(pipe.transform('')).toBe('');
  });

  it('should convert bold markdown', () => {
    const result = pipe.transform('**bold**');
    expect(result).toBeTruthy();
    const html = (result as any).changingThisBreaksApplicationSecurity || result.toString();
    expect(html).toContain('<strong>bold</strong>');
  });

  it('should convert heading markdown', () => {
    const result = pipe.transform('# Heading');
    expect(result).toBeTruthy();
    const html = (result as any).changingThisBreaksApplicationSecurity || result.toString();
    expect(html).toContain('Heading');
  });

  it('should convert links', () => {
    const result = pipe.transform('[link](http://example.com)');
    expect(result).toBeTruthy();
    const html = (result as any).changingThisBreaksApplicationSecurity || result.toString();
    expect(html).toContain('href="http://example.com"');
  });
});
