import React from 'react';
import { Wrench } from 'lucide-react';

export const BrandMark = ({ withWordmark = true, size = 36 }) => (
  <div className="ff-brand">
    <div className="ff-brand-mark" style={{ width: size, height: size }}>
      <Wrench size={Math.round(size * 0.52)} strokeWidth={2.4} />
    </div>
    {withWordmark && (
      <span className="ff-brand-word">
        FixFlow
        <span className="ff-brand-tag">AI</span>
      </span>
    )}
  </div>
);
