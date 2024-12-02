use itertools::Itertools;

#[derive(Debug, Clone)]
pub struct HandPos {
    pub f1: FingerPos,
    pub f2: FingerPos,
    pub f3: FingerPos,
    pub f4: FingerPos,
    pub f5: FingerPos,
}

impl Default for HandPos {
    fn default() -> Self {
        HandPos {
            f1: FingerPos {
                upper: SensorPos(0.0, 1.0, -2.0),
                lower: SensorPos(0.0, 0.0, -2.0),
            },
            f2: FingerPos {
                upper: SensorPos(0.0, 2.0, -1.0),
                lower: SensorPos(0.0, 0.0, -1.0),
            },
            f3: FingerPos {
                upper: SensorPos(0.0, 3.0, 0.0),
                lower: SensorPos(0.0, 0.0, 0.0),
            },
            f4: FingerPos {
                upper: SensorPos(0.0, 2.0, 1.0),
                lower: SensorPos(0.0, 0.0, 1.0),
            },
            f5: FingerPos {
                upper: SensorPos(0.0, 1.0, 2.0),
                lower: SensorPos(0.0, 0.0, 2.0),
            },
        }
    }
}

#[derive(Debug, Clone)]

pub struct FingerPos {
    pub upper: SensorPos,
    pub lower: SensorPos,
}
#[derive(Debug, Clone)]
pub struct SensorPos(pub f32, pub f32, pub f32);

impl HandPos {
    pub fn to_bytes(&self) -> Vec<u8> {
        let mut ret: Vec<u8> = Vec::new();
        for f in &[&self.f1, &self.f2, &self.f3, &self.f4, &self.f5] {
            for part in &[&f.upper, &f.lower] {
                ret.extend_from_slice(&part.0.to_ne_bytes());
                ret.extend_from_slice(&part.1.to_ne_bytes());
                ret.extend_from_slice(&part.2.to_ne_bytes());
            }
        }
        return ret;
    }
}

#[derive(Debug, Clone)]
pub struct HandCommand {
    pub f1: FingerCommand,
    pub f2: FingerCommand,
    pub f3: FingerCommand,
    pub f4: FingerCommand,
    pub f5: FingerCommand,
}

impl HandCommand {
    pub fn from_bytes(bytes: &[u8]) -> Self {
        let mut bytes_iter = bytes
            .into_iter()
            .tuple_windows()
            .map(|(&i1, &i2, &i3, &i4)| [i1, i2, i3, i4]);
        HandCommand {
            f1: FingerCommand {
                upper: PulleyPull(f32::from_ne_bytes(bytes_iter.next().unwrap())),
                lower: PulleyPull(f32::from_ne_bytes(bytes_iter.next().unwrap())),
            },
            f2: FingerCommand {
                upper: PulleyPull(f32::from_ne_bytes(bytes_iter.next().unwrap())),
                lower: PulleyPull(f32::from_ne_bytes(bytes_iter.next().unwrap())),
            },
            f3: FingerCommand {
                upper: PulleyPull(f32::from_ne_bytes(bytes_iter.next().unwrap())),
                lower: PulleyPull(f32::from_ne_bytes(bytes_iter.next().unwrap())),
            },
            f4: FingerCommand {
                upper: PulleyPull(f32::from_ne_bytes(bytes_iter.next().unwrap())),
                lower: PulleyPull(f32::from_ne_bytes(bytes_iter.next().unwrap())),
            },
            f5: FingerCommand {
                upper: PulleyPull(f32::from_ne_bytes(bytes_iter.next().unwrap())),
                lower: PulleyPull(f32::from_ne_bytes(bytes_iter.next().unwrap())),
            },
        }
    }
}
#[derive(Debug, Clone)]
pub struct FingerCommand {
    pub upper: PulleyPull,
    pub lower: PulleyPull,
}
#[derive(Debug, Clone)]
pub struct PulleyPull(pub f32);
