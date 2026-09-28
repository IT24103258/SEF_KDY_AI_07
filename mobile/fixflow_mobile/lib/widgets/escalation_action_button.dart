import 'package:flutter/material.dart';

/// A self-contained Escalate / De-escalate action button for Component 2.
///
/// • When [isEscalated] is false → shows an Escalate button (red).
/// • When [isEscalated] is true  → shows a De-escalate button (teal).
///
/// On tap it opens the appropriate confirmation dialog, then invokes
/// [onEscalate] or [onDeEscalate] with the entered reason (and hazard flag
/// for escalation). The parent widget owns the loading state and passes it
/// in via [isLoading].
class EscalationActionButton extends StatelessWidget {
  /// Whether the request is currently in an escalated state.
  final bool isEscalated;

  /// Whether any management action is currently in flight.
  final bool isLoading;

  /// The human-readable request identifier shown in dialog copy.
  final String requestNumber;

  /// Called when the user confirms escalation.
  /// Arguments: (reason, immediateHazard).
  final Future<void> Function(String reason, bool immediateHazard)? onEscalate;

  /// Called when the user confirms de-escalation.
  /// Argument: reason string.
  final Future<void> Function(String reason)? onDeEscalate;

  const EscalationActionButton({
    super.key,
    required this.isEscalated,
    required this.requestNumber,
    this.isLoading = false,
    this.onEscalate,
    this.onDeEscalate,
  });

  // ─── Escalate dialog ────────────────────────────────────────────────────────

  void _showEscalateDialog(BuildContext context) {
    final reasonController = TextEditingController(
      text: 'Critical priority escalation enforced',
    );
    bool immediateHazard = false;

    showDialog<void>(
      context: context,
      builder: (ctx) => StatefulBuilder(
        builder: (ctx, setDialogState) => AlertDialog(
          shape: RoundedRectangleBorder(borderRadius: BorderRadius.circular(14)),
          title: const Row(
            children: [
              Icon(Icons.warning_rounded, color: Colors.red, size: 22),
              SizedBox(width: 8),
              Text(
                'Escalate Request',
                style: TextStyle(fontWeight: FontWeight.bold, fontSize: 17),
              ),
            ],
          ),
          content: Column(
            mainAxisSize: MainAxisSize.min,
            crossAxisAlignment: CrossAxisAlignment.start,
            children: [
              const Text(
                'Escalate this request?',
                style: TextStyle(fontWeight: FontWeight.bold, fontSize: 14),
              ),
              const SizedBox(height: 4),
              Text(
                'Escalating will mark $requestNumber as escalated, '
                'enforce Critical priority, and assign a 1-hour response SLA.',
                style: const TextStyle(fontSize: 12, color: Colors.grey),
              ),
              const SizedBox(height: 12),
              TextField(
                controller: reasonController,
                decoration: const InputDecoration(
                  labelText: 'Escalation Reason *',
                  border: OutlineInputBorder(),
                  contentPadding:
                      EdgeInsets.symmetric(horizontal: 12, vertical: 10),
                ),
                maxLines: 2,
              ),
              const SizedBox(height: 8),
              CheckboxListTile(
                title: const Text(
                  'Immediate Safety Hazard',
                  style: TextStyle(
                    fontSize: 13,
                    color: Colors.red,
                    fontWeight: FontWeight.w600,
                  ),
                ),
                value: immediateHazard,
                contentPadding: EdgeInsets.zero,
                dense: true,
                onChanged: (val) =>
                    setDialogState(() => immediateHazard = val ?? false),
              ),
            ],
          ),
          actions: [
            TextButton(
              onPressed: () => Navigator.pop(ctx),
              child: const Text('Cancel'),
            ),
            ElevatedButton(
              key: const Key('escalate_confirm_button'),
              style: ElevatedButton.styleFrom(
                backgroundColor: Colors.red.shade700,
                foregroundColor: Colors.white,
                shape: RoundedRectangleBorder(
                    borderRadius: BorderRadius.circular(8)),
              ),
              onPressed: () {
                final reason = reasonController.text.trim();
                if (reason.isEmpty) return;
                Navigator.pop(ctx);
                onEscalate?.call(reason, immediateHazard);
              },
              child: const Text('Escalate',
                  style: TextStyle(fontWeight: FontWeight.bold)),
            ),
          ],
        ),
      ),
    );
  }

  // ─── De-escalate dialog ─────────────────────────────────────────────────────

  void _showDeEscalateDialog(BuildContext context) {
    final reasonController = TextEditingController(
      text: 'Hazard resolved, recalculated to standard SLA',
    );

    showDialog<void>(
      context: context,
      builder: (ctx) => AlertDialog(
        key: const Key('deescalate_dialog'),
        shape: RoundedRectangleBorder(borderRadius: BorderRadius.circular(14)),
        title: const Row(
          children: [
            Icon(Icons.check_circle_outline_rounded,
                color: Colors.teal, size: 22),
            SizedBox(width: 8),
            Text(
              'De-escalate Request',
              style: TextStyle(fontWeight: FontWeight.bold, fontSize: 17),
            ),
          ],
        ),
        content: Column(
          mainAxisSize: MainAxisSize.min,
          crossAxisAlignment: CrossAxisAlignment.start,
          children: [
            const Text(
              'De-escalate this request?',
              style: TextStyle(fontWeight: FontWeight.bold, fontSize: 14),
            ),
            const SizedBox(height: 4),
            Text(
              'This will remove the current escalation from $requestNumber '
              'and recalculate priority from current request facts.',
              style: const TextStyle(fontSize: 12, color: Colors.grey),
            ),
            const SizedBox(height: 12),
            TextField(
              key: const Key('deescalate_reason_field'),
              controller: reasonController,
              decoration: const InputDecoration(
                labelText: 'De-escalation Reason *',
                border: OutlineInputBorder(),
                contentPadding:
                    EdgeInsets.symmetric(horizontal: 12, vertical: 10),
              ),
              maxLines: 2,
            ),
          ],
        ),
        actions: [
          TextButton(
            key: const Key('deescalate_cancel_button'),
            onPressed: () => Navigator.pop(ctx),
            child: const Text('Cancel'),
          ),
          ElevatedButton(
            key: const Key('deescalate_confirm_button'),
            style: ElevatedButton.styleFrom(
              backgroundColor: Colors.teal.shade700,
              foregroundColor: Colors.white,
              shape: RoundedRectangleBorder(
                  borderRadius: BorderRadius.circular(8)),
            ),
            onPressed: () {
              final reason = reasonController.text.trim();
              if (reason.isEmpty) return;
              Navigator.pop(ctx);
              onDeEscalate?.call(reason);
            },
            child: const Text('De-escalate',
                style: TextStyle(fontWeight: FontWeight.bold)),
          ),
        ],
      ),
    );
  }

  // ─── Build ──────────────────────────────────────────────────────────────────

  @override
  Widget build(BuildContext context) {
    if (isEscalated) {
      // De-escalate button — teal
      return ElevatedButton.icon(
        key: const Key('deescalate_button'),
        style: ElevatedButton.styleFrom(
          backgroundColor: Colors.teal.shade700,
          foregroundColor: Colors.white,
          padding: const EdgeInsets.symmetric(horizontal: 14, vertical: 10),
          shape:
              RoundedRectangleBorder(borderRadius: BorderRadius.circular(8)),
        ),
        icon: isLoading
            ? const SizedBox(
                width: 14,
                height: 14,
                child: CircularProgressIndicator(
                    strokeWidth: 2, color: Colors.white),
              )
            : const Icon(Icons.arrow_downward, size: 16),
        label: const Text(
          'De-escalate',
          style: TextStyle(fontWeight: FontWeight.bold, fontSize: 13),
        ),
        onPressed:
            isLoading ? null : () => _showDeEscalateDialog(context),
      );
    }

    // Escalate button — red
    return ElevatedButton.icon(
      key: const Key('escalate_button'),
      style: ElevatedButton.styleFrom(
        backgroundColor: Colors.red.shade700,
        foregroundColor: Colors.white,
        padding: const EdgeInsets.symmetric(horizontal: 14, vertical: 10),
        shape: RoundedRectangleBorder(borderRadius: BorderRadius.circular(8)),
      ),
      icon: const Icon(Icons.arrow_upward, size: 16),
      label: const Text(
        'Escalate',
        style: TextStyle(fontWeight: FontWeight.bold, fontSize: 13),
      ),
      onPressed: isLoading ? null : () => _showEscalateDialog(context),
    );
  }
}
