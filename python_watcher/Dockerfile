FROM python:3.11-slim

WORKDIR /app

# Copy requirements
COPY requirements.txt .
RUN pip install --no-cache-dir -r requirements.txt

# Copy source
COPY src/ src/
COPY config.json config.json
COPY config_reports.json config_reports.json

# Create logs directory
RUN mkdir -p logs

# Set Python path so src modules can be imported
ENV PYTHONPATH=/app/src

# Default command (can be overridden)
CMD ["python", "src/rss_watcher.py"]
