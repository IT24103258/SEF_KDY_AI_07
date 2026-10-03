import 'dart:math' as math;

import 'package:flutter/material.dart';

import 'priority_badge.dart';

/// Animated display of the authoritative, C#-validated Component 2 risk score.
///
/// This widget performs **no** scoring. It receives the already validated score
/// and level from the backend assessment and only animates the reveal from 0 up
/// to that value. The risk bands drawn behind the arc are the fixed backend
/// thresholds (1-25 Low, 26-50 Medium, 51-75 High, 76-100 Critical) rendered as
/// a scale — they are never used to derive a level.
class RiskGaugeWidget extends StatefulWidget {
  /// Validated risk score, or `null` when the assessment failed safely and the
  /// backend produced no score. A missing score is never estimated here.
  final int? riskScore;

  /// Validated risk level string from the same assessment.
  final String? riskLevel;

  const RiskGaugeWidget({
    super.key,
    required this.riskScore,
    this.riskLevel,
  });

  @override
  State<RiskGaugeWidget> createState() => _RiskGaugeWidgetState();
}

class _RiskGaugeWidgetState extends State<RiskGaugeWidget>
    with SingleTickerProviderStateMixin {
  late final AnimationController _controller;
  late Animation<double> _animation;

  @override
  void initState() {
    super.initState();
    _controller = AnimationController(
      vsync: this,
      duration: const Duration(milliseconds: 900),
    );
    _animate(from: 0);
  }

  @override
  void didUpdateWidget(covariant RiskGaugeWidget oldWidget) {
    super.didUpdateWidget(oldWidget);
    if (oldWidget.riskScore != widget.riskScore ||
        oldWidget.riskLevel != widget.riskLevel) {
      _animate(from: _animation.value);
    }
  }

  @override
  void dispose() {
    _controller.dispose();
    super.dispose();
  }

  void _animate({required double from}) {
    _animation = Tween<double>(
      begin: from,
      end: (widget.riskScore ?? 0).toDouble(),
    ).animate(CurvedAnimation(parent: _controller, curve: Curves.easeOutCubic));
    _controller
      ..reset()
      ..forward();
  }

  static Color levelColor(String? level) {
    switch (level?.trim().toLowerCase()) {
      case 'critical':
        return _RiskGaugeBand.critical;
      case 'high':
        return _RiskGaugeBand.high;
      case 'medium':
        return _RiskGaugeBand.medium;
      case 'low':
        return _RiskGaugeBand.low;
      default:
        return Colors.grey;
    }
  }

  @override
  Widget build(BuildContext context) {
    final score = widget.riskScore;
    final level = widget.riskLevel?.trim() ?? '';

    return Card(
      elevation: 0,
      shape: RoundedRectangleBorder(
        borderRadius: BorderRadius.circular(10),
        side: BorderSide(color: Colors.grey.withValues(alpha: 0.25)),
      ),
      child: Padding(
        padding: const EdgeInsets.fromLTRB(12, 12, 12, 10),
        child: Column(
          crossAxisAlignment: CrossAxisAlignment.start,
          children: [
            Row(
              children: [
                const Icon(Icons.speed_outlined, size: 16, color: Colors.grey),
                const SizedBox(width: 6),
                const Text(
                  'ANIMATED RISK GAUGE',
                  style: TextStyle(
                    fontWeight: FontWeight.bold,
                    fontSize: 11,
                    letterSpacing: 0.6,
                    color: Colors.grey,
                  ),
                ),
                const Spacer(),
                if (score != null && level.isNotEmpty)
                  RiskLevelBadgeWidget(riskLevel: level),
              ],
            ),
            const SizedBox(height: 4),
            if (score == null) _buildUnavailable() else _buildGauge(score),
          ],
        ),
      ),
    );
  }

  Widget _buildUnavailable() {
    return const Padding(
      padding: EdgeInsets.symmetric(vertical: 14),
      child: Row(
        children: [
          Icon(Icons.block, color: Colors.grey, size: 30),
          SizedBox(width: 12),
          Expanded(
            child: Column(
              crossAxisAlignment: CrossAxisAlignment.start,
              children: [
                Text(
                  'RISK SCORE UNAVAILABLE',
                  style: TextStyle(
                    fontWeight: FontWeight.bold,
                    fontSize: 12.5,
                    color: Colors.grey,
                  ),
                ),
                SizedBox(height: 2),
                Text(
                  'The Component 2 assessment failed safely, so no validated '
                  'score or level was produced. Nothing is estimated here.',
                  style: TextStyle(fontSize: 11, color: Colors.grey),
                ),
              ],
            ),
          ),
        ],
      ),
    );
  }

  Widget _buildGauge(int score) {
    final color = levelColor(widget.riskLevel);

    return Column(
      crossAxisAlignment: CrossAxisAlignment.start,
      children: [
        SizedBox(
          height: 112,
          child: Stack(
            alignment: Alignment.bottomCenter,
            children: [
              Positioned.fill(
                child: AnimatedBuilder(
                  animation: _animation,
                  builder: (context, _) => CustomPaint(
                    painter: _RiskGaugePainter(
                      value: _animation.value,
                      arcColor: color,
                    ),
                  ),
                ),
              ),
              AnimatedBuilder(
                animation: _animation,
                builder: (context, _) => Padding(
                  padding: const EdgeInsets.only(bottom: 2),
                  child: Column(
                    mainAxisSize: MainAxisSize.min,
                    children: [
                      Text(
                        '${_animation.value.round()}',
                        style: TextStyle(
                          fontSize: 30,
                          height: 1.05,
                          fontWeight: FontWeight.bold,
                          color: color,
                        ),
                      ),
                      const Text(
                        '/ 100',
                        style: TextStyle(fontSize: 10.5, color: Colors.grey),
                      ),
                    ],
                  ),
                ),
              ),
            ],
          ),
        ),
        const SizedBox(height: 6),
        _buildBandLegend(),
        const SizedBox(height: 6),
        const Text(
          'Shows the validated score persisted by the backend. The app does not '
          'recalculate it.',
          style: TextStyle(fontSize: 10, color: Colors.grey),
        ),
      ],
    );
  }

  Widget _buildBandLegend() {
    final active = widget.riskLevel?.trim().toLowerCase();
    const bands = <String, String>{
      'low': '1-25 LOW',
      'medium': '26-50 MEDIUM',
      'high': '51-75 HIGH',
      'critical': '76-100 CRITICAL',
    };

    return Wrap(
      spacing: 6,
      runSpacing: 6,
      children: bands.entries.map((entry) {
        final color = levelColor(entry.key);
        final isActive = entry.key == active;
        return Container(
          key: ValueKey('risk_band_${entry.key}'),
          padding: const EdgeInsets.symmetric(horizontal: 7, vertical: 3),
          decoration: BoxDecoration(
            color: isActive ? color.withValues(alpha: 0.16) : Colors.transparent,
            borderRadius: BorderRadius.circular(6),
            border: Border.all(
              color: isActive ? color : Colors.grey.withValues(alpha: 0.3),
            ),
          ),
          child: Text(
            entry.value,
            style: TextStyle(
              fontSize: 9.5,
              fontWeight: isActive ? FontWeight.bold : FontWeight.normal,
              color: isActive ? color : Colors.grey,
            ),
          ),
        );
      }).toList(),
    );
  }
}

class _RiskGaugePainter extends CustomPainter {
  final double value;
  final Color arcColor;

  _RiskGaugePainter({required this.value, required this.arcColor});

  static const double _stroke = 13;

  @override
  void paint(Canvas canvas, Size size) {
    final centre = Offset(size.width / 2, size.height - _stroke / 2);
    final radius = math.max(
      8.0,
      math.min((size.width - _stroke) / 2, size.height - _stroke),
    );
    final rect = Rect.fromCircle(center: centre, radius: radius);

    final track = Paint()
      ..style = PaintingStyle.stroke
      ..strokeWidth = _stroke
      ..strokeCap = StrokeCap.butt;

    _drawBand(canvas, rect, track, 0, 25, _RiskGaugeBand.low);
    _drawBand(canvas, rect, track, 25, 50, _RiskGaugeBand.medium);
    _drawBand(canvas, rect, track, 50, 75, _RiskGaugeBand.high);
    _drawBand(canvas, rect, track, 75, 100, _RiskGaugeBand.critical);

    final clamped = value.clamp(0.0, 100.0);
    if (clamped > 0) {
      final arc = Paint()
        ..style = PaintingStyle.stroke
        ..strokeWidth = _stroke
        ..strokeCap = StrokeCap.round
        ..color = arcColor;
      canvas.drawArc(rect, math.pi, (clamped / 100) * math.pi, false, arc);
    }

    final angle = math.pi + (clamped / 100) * math.pi;
    final marker = Offset(
      centre.dx + radius * math.cos(angle),
      centre.dy + radius * math.sin(angle),
    );
    canvas.drawCircle(marker, _stroke / 2 + 2, Paint()..color = Colors.white);
    canvas.drawCircle(marker, _stroke / 2 - 1, Paint()..color = arcColor);
  }

  void _drawBand(
    Canvas canvas,
    Rect rect,
    Paint paint,
    double from,
    double to,
    Color color,
  ) {
    canvas.drawArc(
      rect,
      math.pi + (from / 100) * math.pi,
      ((to - from) / 100) * math.pi,
      false,
      paint..color = color.withValues(alpha: 0.16),
    );
  }

  @override
  bool shouldRepaint(covariant _RiskGaugePainter oldDelegate) =>
      oldDelegate.value != value || oldDelegate.arcColor != arcColor;
}

class _RiskGaugeBand {
  static const Color low = Color(0xFF10B981);
  static const Color medium = Color(0xFF2563EB);
  static const Color high = Color(0xFFF59E0B);
  static const Color critical = Color(0xFFDC2626);
}
