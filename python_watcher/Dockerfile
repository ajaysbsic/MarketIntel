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

# Default command - run the report watcher (v3 with intelligent crawler)
CMD ["python", "src/report_watcher_v3.py"]
