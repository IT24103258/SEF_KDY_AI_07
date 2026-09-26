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
  bool _isStopped = true;

  int get elapsedSeconds {
    if (_isRunning && _lastStartTime != null) {
      final currentSession = DateTime.now().difference(_lastStartTime!);
      return (_accumulatedTime + currentSession).inSeconds;
    }
    return _accumulatedTime.inSeconds;
  }

  bool get isRunning => _isRunning;
  bool get isPaused =>
      !_isRunning && !_isStopped && _accumulatedTime.inSeconds > 0;
  bool get isStopped => !_isRunning && _isStopped;

  @override
  void initState() {
    super.initState();
    _accumulatedTime = Duration(seconds: widget.initialElapsedSeconds);
  }

  @override
  void dispose() {
    _periodicTimer?.cancel();
    super.dispose();
  }

  void startTimer() {
    if (_isRunning) return;

    setState(() {
      _isRunning = true;
      _isStopped = false;
      _lastStartTime = DateTime.now();
    });

    _periodicTimer?.cancel();
    _periodicTimer = Timer.periodic(const Duration(milliseconds: 500), (_) {
      if (!mounted) return;
      setState(() {});
      widget.onTick?.call(elapsedSeconds);
    });

    widget.onStateChanged?.call();
  }

  void pauseTimer() => _endActiveSession(stopped: false);

  void stopTimer() => _endActiveSession(stopped: true);

  void _endActiveSession({required bool stopped}) {
    if (_isRunning && _lastStartTime != null) {
      _accumulatedTime += DateTime.now().difference(_lastStartTime!);
    }

    _periodicTimer?.cancel();
    _periodicTimer = null;

    setState(() {
      _isRunning = false;
      _isStopped = stopped;
      _lastStartTime = null;
    });

    widget.onTick?.call(elapsedSeconds);
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
    final primaryActionLabel = _isRunning
        ? 'Pause'
        : currentSeconds == 0
            ? 'Start'
            : 'Resume';

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
                  color: _isRunning
                      ? Colors.greenAccent
                      : isPaused
                          ? Colors.amberAccent
                          : Colors.white70,
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
              ElevatedButton.icon(
                onPressed: _isRunning ? pauseTimer : startTimer,
                icon:
                    Icon(_isRunning ? Icons.pause : Icons.play_arrow, size: 18),
                label: Text(primaryActionLabel),
                style: ElevatedButton.styleFrom(
                  backgroundColor: _isRunning
                      ? Colors.amber.shade700
                      : Colors.green.shade600,
                  foregroundColor: Colors.white,
                  padding:
                      const EdgeInsets.symmetric(horizontal: 20, vertical: 10),
                ),
              ),
              const SizedBox(width: 12),
              OutlinedButton.icon(
                onPressed: _isStopped ? null : stopTimer,
                icon: const Icon(Icons.stop_outlined, size: 18),
                label: const Text('Stop'),
                style: OutlinedButton.styleFrom(
                  foregroundColor: Colors.white,
                  side: const BorderSide(color: Colors.white70),
                  padding:
                      const EdgeInsets.symmetric(horizontal: 16, vertical: 10),
                ),
              ),
            ],
          ),
        ],
      ),
    );
  }
}
