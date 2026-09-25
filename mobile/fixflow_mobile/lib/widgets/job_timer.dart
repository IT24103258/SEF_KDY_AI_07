import 'dart:async';
import 'package:flutter/material.dart';
import '../core/theme/app_colors.dart';

class JobTimer extends StatefulWidget {
  final int initialElapsedSeconds;
  final ValueChanged<int>? onTick;
  final VoidCallback? onStateChanged;

  const JobTimer({
    super.key,
    this.initialElapsedSeconds = 0,
    this.onTick,
    this.onStateChanged,
  });

  @override
  State<JobTimer> createState() => JobTimerState();
}

class JobTimerState extends State<JobTimer> {
  Timer? _periodicTimer;
  DateTime? _lastStartTime;
  Duration _accumulatedTime = Duration.zero;
  bool _isRunning = false;

  int get elapsedSeconds {
    if (_isRunning && _lastStartTime != null) {
      final currentSession = DateTime.now().difference(_lastStartTime!);
      return (_accumulatedTime + currentSession).inSeconds;
    }
    return _accumulatedTime.inSeconds;
  }

  bool get isRunning => _isRunning;
  bool get isPaused => !_isRunning && _accumulatedTime.inSeconds > 0;

  @override
  void initState() {
    super.initState();
    _accumulatedTime = Duration(seconds: widget.initialElapsedSeconds);
    // Auto-start on mount if desired
    startTimer();
  }

  @override
  void dispose() {
    _periodicTimer?.cancel();
    _periodicTimer = null;
    super.dispose();
  }

  void startTimer() {
    if (_isRunning) return;
    setState(() {
      _isRunning = true;
      _lastStartTime = DateTime.now();
    });

    _periodicTimer?.cancel();
    _periodicTimer = Timer.periodic(const Duration(milliseconds: 500), (_) {
      if (mounted) {
        setState(() {});
        widget.onTick?.call(elapsedSeconds);
      }
    });

    widget.onStateChanged?.call();
  }

  void pauseTimer() {
    if (!_isRunning) return;
    if (_lastStartTime != null) {
      _accumulatedTime += DateTime.now().difference(_lastStartTime!);
    }

    _periodicTimer?.cancel();
    _periodicTimer = null;

    setState(() {
      _isRunning = false;
      _lastStartTime = null;
    });

    widget.onTick?.call(elapsedSeconds);
    widget.onStateChanged?.call();
  }

  void resetTimer() {
    _periodicTimer?.cancel();
    _periodicTimer = null;

    setState(() {
      _isRunning = false;
      _lastStartTime = null;
      _accumulatedTime = Duration.zero;
    });

    widget.onTick?.call(0);
    widget.onStateChanged?.call();
  }

  String formatTime(int totalSeconds) {
    final hours = totalSeconds ~/ 3600;
    final minutes = (totalSeconds % 3600) ~/ 60;
    final seconds = totalSeconds % 60;
    return '${hours.toString().padLeft(2, '0')}:${minutes.toString().padLeft(2, '0')}:${seconds.toString().padLeft(2, '0')}';
  }

  @override
  Widget build(BuildContext context) {
    final currentSeconds = elapsedSeconds;
    final formattedTime = formatTime(currentSeconds);

    return Container(
      padding: const EdgeInsets.symmetric(vertical: 20, horizontal: 16),
      decoration: BoxDecoration(
        color: AppColors.primary,
        borderRadius: BorderRadius.circular(12),
        boxShadow: [
          BoxShadow(
            color: AppColors.primary.withOpacity(0.3),
            blurRadius: 10,
            offset: const Offset(0, 4),
          ),
        ],
      ),
      child: Column(
        children: [
          Row(
            mainAxisAlignment: MainAxisAlignment.center,
            children: [
              Container(
                width: 8,
                height: 8,
                decoration: BoxDecoration(
                  color: _isRunning ? Colors.greenAccent : (isPaused ? Colors.amberAccent : Colors.white70),
                  shape: BoxShape.circle,
                ),
              ),
              const SizedBox(width: 8),
              Text(
                _isRunning
                    ? 'JOB TIMER ACTIVE'
                    : isPaused
                        ? 'TIMER PAUSED'
                        : 'TIMER STOPPED',
                style: const TextStyle(
                  color: Colors.white70,
                  fontSize: 12,
                  fontWeight: FontWeight.bold,
                  letterSpacing: 1.2,
                ),
              ),
            ],
          ),
          const SizedBox(height: 12),
          Text(
            formattedTime,
            style: const TextStyle(
              color: Colors.white,
              fontSize: 38,
              fontWeight: FontWeight.bold,
              fontFamily: 'monospace',
              letterSpacing: 2,
            ),
          ),
          const SizedBox(height: 16),
          Row(
            mainAxisAlignment: MainAxisAlignment.center,
            children: [
              if (_isRunning)
                ElevatedButton.icon(
                  onPressed: pauseTimer,
                  icon: const Icon(Icons.pause, size: 18),
                  label: const Text('Pause'),
                  style: ElevatedButton.styleFrom(
                    backgroundColor: Colors.amber.shade700,
                    foregroundColor: Colors.white,
                    padding: const EdgeInsets.symmetric(horizontal: 20, vertical: 10),
                  ),
                )
              else
                ElevatedButton.icon(
                  onPressed: startTimer,
                  icon: const Icon(Icons.play_arrow, size: 18),
                  label: Text(isPaused ? 'Resume' : 'Start'),
                  style: ElevatedButton.styleFrom(
                    backgroundColor: Colors.green.shade600,
                    foregroundColor: Colors.white,
                    padding: const EdgeInsets.symmetric(horizontal: 20, vertical: 10),
                  ),
                ),
              const SizedBox(width: 12),
              OutlinedButton.icon(
                onPressed: (currentSeconds > 0) ? resetTimer : null,
                icon: const Icon(Icons.refresh, size: 18),
                label: const Text('Reset'),
                style: OutlinedButton.styleFrom(
                  foregroundColor: Colors.white,
                  side: const BorderSide(color: Colors.white70),
                  padding: const EdgeInsets.symmetric(horizontal: 16, vertical: 10),
                ),
              ),
            ],
          ),
        ],
      ),
    );
  }
}
